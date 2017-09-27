using System;
using System.IO;

namespace FlatFiles
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomWriter : IWriter
    {
        private readonly CustomRecordWriter recordWriter;
        private bool isFirstLine;

        /// <summary>
        /// Initializes a new CustomBuilder with the given schema.
        /// </summary>
        /// <param name="writer">A writer over the fixed-length document.</param>
        /// <param name="schema">The schema of the fixed-length document.</param>
        /// <param name="options">The options used to format the output.</param>
        /// <exception cref="ArgumentNullException">The writer is null.</exception>
        /// <exception cref="ArgumentNullException">The schema is null.</exception>
        public CustomWriter(TextWriter writer, CustomSchema schema, CustomOptions options = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }
            if (options == null)
            {
                options = new CustomOptions();
            }
            this.recordWriter = new CustomRecordWriter(writer, schema, options);
            this.isFirstLine = true;
        }

        /// <summary>
        /// Gets the schema used to build the output.
        /// </summary>
        /// <returns>The schema used to build the output.</returns>
        public CustomSchema GetSchema()
        {
            return recordWriter.Schema;
        }

        ISchema IWriter.GetSchema()
        {
            return GetSchema();
        }

        /// <summary>
        /// Writes the textual representation of the given values to the writer.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <exception cref="ArgumentNullException">The values array is null.</exception>
        public void Write(object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            recordWriter.WriteRecord(values);
            recordWriter.WriteRecordSeparator();
        }
    }
}
