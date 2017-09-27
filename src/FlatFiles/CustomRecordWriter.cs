using FlatFiles.Resources;
using System;
using System.IO;
using System.Linq;

namespace FlatFiles
{
    internal class CustomRecordWriter
    {
        private readonly TextWriter writer;
        private readonly CustomSchema schema;
        private readonly CustomOptions options;
        private readonly string quoteString;
        private readonly string doubleQuoteString;

        public CustomRecordWriter(TextWriter writer, CustomSchema schema, CustomOptions options)
        {
            this.writer = writer;
            this.schema = schema;
            this.options = options.Clone();
            this.quoteString = String.Empty + options.Quote;
            this.doubleQuoteString = String.Empty + options.Quote + options.Quote;
        }

        public CustomSchema Schema
        {
            get { return schema; }
        }

        public CustomOptions Options
        {
            get { return options; }
        }

        public void WriteRecord(object[] values)
        {
            if (values.Length != schema.ColumnDefinitions.HandledCount)
            {
                throw new ArgumentException(SharedResources.WrongNumberOfValues, "values");
            }
            var formattedColumns = schema.FormatValues(values);
            var fittedColumns = formattedColumns.Select((v, i) => fitWidth(schema.Windows[i], escape(v)));
            string joined = String.Join(options.Separator, fittedColumns) + options.Separator;
            writer.Write(joined);
        }

        public void WriteSchema()
        {
            var names = schema.ColumnDefinitions.Select(c => c.ColumnName);
            var fitted = names.Select((v, i) => fitWidth(schema.Windows[i], v));
            foreach (string column in fitted)
            {
                writer.Write(column);
            }
        }

        private string fitWidth(Window window, string value)
        {
            if (value == null)
            {
                value = String.Empty;
            }
            if (value.Length > window.Width)
            {
                return getTruncatedValue(value, window);
            }
            else if (value.Length < window.Width)
            {
                return getPaddedValue(value, window);
            }
            else
            {
                return value;
            }
        }

        private string getTruncatedValue(string value, Window window)
        {
            OverflowTruncationPolicy policy = window.TruncationPolicy ?? options.TruncationPolicy;
            if (policy == OverflowTruncationPolicy.TruncateLeading)
            {
                int start = value.Length - window.Width;  // take characters on the end
                return value.Substring(start, window.Width);
            }
            else
            {
                return value.Substring(0, window.Width);
            }
        }

        private string getPaddedValue(string value, Window window)
        {
            if (window.Alignment == FixedAlignment.LeftAligned)
            {
                return value.PadRight(window.Width, window.FillCharacter ?? options.FillCharacter);
            }
            else
            {
                return value.PadLeft(window.Width, window.FillCharacter ?? options.FillCharacter);
            }
        }
        private string escape(string value)
        {
            if (needsEscaped(value))
            {
                return quoteString + value.Replace(quoteString, doubleQuoteString) + quoteString;
            }
            else
            {
                return value;
            }
        }

        private bool needsEscaped(string value)
        {
            // Never escape null.
            if (value == null)
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(options.Separator))
            {
                return false;
            }
            if (options.QuoteBehavior == QuoteBehavior.AlwaysQuote)
            {
                return true;
            }
            // Don't escape empty strings.
            if (value == String.Empty)
            {
                return false;
            }
            // Escape strings beginning or ending in whitespace.
            if (Char.IsWhiteSpace(value[0]) || Char.IsWhiteSpace(value[value.Length - 1]))
            {
                return true;
            }
            // Escape strings containing the separator.
            if (value.Contains(options.Separator))
            {
                return true;
            }
            // Escape strings containing the record separator.
            if (value.Contains(options.RecordSeparator))
            {
                return true;
            }
            // Escape strings containing quotes.
            if (value.Contains(quoteString))
            {
                return true;
            }
            return false;
        }

        public void WriteRecordSeparator()
        {
            if (String.IsNullOrEmpty(options.RecordSeparator))
            {
                return;
            }
            writer.Write(options.RecordSeparator);
        }
    }
}
