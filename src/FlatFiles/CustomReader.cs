using FlatFiles.Resources;
using System;
using System.IO;
using System.Text;

namespace FlatFiles
{
    /// <summary>
    /// Extracts records from a file that has value in fixed-length columns.
    /// </summary>
    public class CustomReader : IReader
    {
        private readonly CustomRecordParser parser;
        private readonly CustomSchema schema;
        private readonly CustomOptions options;
        private int recordCount;
        private object[] values;
        private bool endOfFile;
        private bool hasError;

        /// <summary>
        /// Initializes a new CustomReader with the given schema.
        /// </summary>
        /// <param name="reader">A reader over the fixed-length document.</param>
        /// <param name="schema">The schema of the fixed-length document.</param>
        /// <param name="options">The options controlling how the fixed-length document is read.</param>
        /// <exception cref="ArgumentNullException">The reader is null.</exception>
        /// <exception cref="ArgumentNullException">The schema is null.</exception>
        public CustomReader(TextReader reader, CustomSchema schema, CustomOptions options = null)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }
            if (options == null)
            {
                options = new CustomOptions();
            }
            parser = new CustomRecordParser(reader, schema, options);
            this.schema = schema;
            this.options = options.Clone();
            if (this.options.IsFirstRecordHeader)
            {
                skip();
            }
        }

        /// <summary>
        /// Gets the schema being used by the parser.
        /// </summary>
        /// <returns>The schema being used by the parser.</returns>
        public CustomSchema GetSchema()
        {
            return schema;
        }

        ISchema IReader.GetSchema()
        {
            return GetSchema();
        }

        /// <summary>
        /// Reads the next record from the file.
        /// </summary>
        /// <returns>True if the next record was parsed; otherwise, false if all files are read.</returns>
        public bool Read()
        {
            if (hasError)
            {
                throw new InvalidOperationException(SharedResources.ReadingWithErrors);
            }
            try
            {
                values = parsePartitions();
                return values != null;
            }
            catch (FlatFileException)
            {
                hasError = true;
                throw;
            }
        }

        private object[] parsePartitions()
        {
            string[] rawValues = partitionWithFilter();
            while (rawValues != null)
            {
                object[] values = parseValues(rawValues);
                if (values != null)
                {
                    return values;
                }
                rawValues = partitionWithFilter();
            }
            return null;
        }

        private string[] partitionWithFilter()
        {
            string record = readWithFilter();
            string[] rawValues = partitionRecord(record);
            while (rawValues != null && options.PartitionedRecordFilter != null && options.PartitionedRecordFilter(rawValues))
            {
                record = readWithFilter();
                rawValues = partitionRecord(record);
            }
            return rawValues;
        }

        private string readWithFilter()
        {
            string record = readNextRecord();
            while (record != null && options.UnpartitionedRecordFilter != null && options.UnpartitionedRecordFilter(record))
            {
                record = readNextRecord();
            }
            return record;
        }

        private object[] parseValues(string[] rawValues)
        {
            try
            {
                return schema.ParseValues(rawValues);
            }
            catch (FlatFileException exception)
            {
                processError(new RecordProcessingException(recordCount, SharedResources.InvalidRecordConversion, exception));
                return null;
            }
        }

        /// <summary>
        /// Skips the next record from the file.
        /// </summary>
        /// <returns>True if the next record was skipped; otherwise, false if all records are read.</returns>
        /// <remarks>The previously parsed values remain available.</remarks>
        public bool Skip()
        {
            if (hasError)
            {
                throw new InvalidOperationException(SharedResources.ReadingWithErrors);
            }
            return skip();
        }

        private bool skip()
        {
            string record = readNextRecord();
            return record != null;
        }

        private string[] partitionRecord(string record)
        {
            if (record == null)
            {
                return null;
            }
            if (record.Length < schema.TotalWidth)
            {
                processError(new RecordProcessingException(recordCount, SharedResources.RecordTooShort));
                return null;
            }
            WindowCollection windows = schema.Windows;
            string[] values = new string[windows.Count];
            int offset = 0;
            for (int index = 0; index != values.Length; ++index)
            {
                Window window = windows[index];
                string value = record.Substring(offset, window.Width);
                if (window.Alignment == FixedAlignment.LeftAligned)
                {
                    value = value.TrimEnd(window.FillCharacter ?? options.FillCharacter);
                }
                else
                {
                    value = value.TrimStart(window.FillCharacter ?? options.FillCharacter);
                }
                values[index] = value;
                offset += window.Width;
            }
            return values;
        }

        private string readNextRecord()
        {
            if (parser.EndOfStream)
            {
                endOfFile = true;
                return null;
            }
            string record = parser.ReadRecord();
            ++recordCount;
            return record;
        }

        private void processError(RecordProcessingException exception)
        {
            if (options.ErrorHandler != null)
            {
                var args = new ProcessingErrorEventArgs(exception);
                options.ErrorHandler(this, args);
                if (args.IsHandled)
                {
                    return;
                }
            }
            throw exception;
        }

        /// <summary>
        /// Gets the values for the current record.
        /// </summary>
        /// <returns>The values of the current record.</returns>
        public object[] GetValues()
        {
            if (hasError)
            {
                throw new InvalidOperationException(SharedResources.ReadingWithErrors);
            }
            if (recordCount == 0)
            {
                throw new InvalidOperationException(SharedResources.ReadNotCalled);
            }
            if (endOfFile)
            {
                throw new InvalidOperationException(SharedResources.NoMoreRecords);
            }
            object[] copy = new object[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }
    }
    internal class CustomRecordParser
    {
        private readonly IRecordReader recordReader;

        public CustomRecordParser(TextReader reader, CustomSchema schema, CustomOptions options)
        {
            if (String.IsNullOrEmpty(options.RecordSeparator))
            {
                this.recordReader = new CustomRecordReader(reader, schema.TotalWidth);
            }
            else
            {
                this.recordReader = new SeparatorRecordReader(reader, options.RecordSeparator);
            }
        }

        public bool EndOfStream
        {
            get { return recordReader.EndOfStream; }
        }

        public string ReadRecord()
        {
            return recordReader.ReadRecord();
        }

        private interface IRecordReader
        {
            bool EndOfStream { get; }

            string ReadRecord();
        }

        private class SeparatorRecordReader : IRecordReader
        {
            private readonly TextReader reader;
            private readonly string separator;

            public SeparatorRecordReader(TextReader reader, string separator)
            {
                this.reader = reader;
                this.separator = separator;
            }

            public bool EndOfStream
            {
                get { return reader.Peek() == -1; }
            }

            public string ReadRecord()
            {
                StringBuilder builder = new StringBuilder();
                int positionIndex = 0;
                while (reader.Peek() != -1 && positionIndex != separator.Length)
                {
                    char next = (char)reader.Read();
                    if (next == separator[positionIndex])
                    {
                        ++positionIndex;
                    }
                    else
                    {
                        positionIndex = 0;
                    }
                    builder.Append(next);
                }
                if (positionIndex == separator.Length)
                {
                    builder.Length -= separator.Length;
                }
                return builder.ToString();
            }
        }

        private class CustomRecordReader : IRecordReader
        {
            private readonly TextReader reader;
            private readonly char[] buffer;

            public CustomRecordReader(TextReader reader, int totalWidth)
            {
                this.reader = reader;
                this.buffer = new char[totalWidth];
            }

            public bool EndOfStream
            {
                get { return reader.Peek() == -1; }
            }

            public string ReadRecord()
            {
                int length = reader.ReadBlock(buffer, 0, buffer.Length);
                return new String(buffer, 0, length);
            }
        }
    }

}
