﻿using System;
using System.IO;

namespace FlatFiles
{
    /// <summary>
    /// Represents a string column that has contains multiple, nested values
    /// </summary>
    public class CustomComplexColumn : ColumnDefinition
    {
        private readonly CustomSchema schema;
        private CustomOptions options;

        /// <summary>
        /// Initializes a new CustomComplexColumn with the given schema and default options.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="schema">The schema of the data embedded in the column.</param>
        public CustomComplexColumn(string columnName, CustomSchema schema)
            : this(columnName, schema, new CustomOptions())
        {
        }

        /// <summary>
        /// Initializes a new CustomComplexColumn with the given schema and options.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="schema">The schema of the data embedded in the column.</param>
        /// <param name="options">The options to use when parsing the embedded data.</param>
        public CustomComplexColumn(string columnName, CustomSchema schema, CustomOptions options)
            : base(columnName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            this.schema = schema;
            this.options = options.Clone();
        }

        /// <summary>
        /// Gets the type of the values in the column.
        /// </summary>
        public override Type ColumnType
        {
            get { return typeof(object[]); }
        }

        /// <summary>
        /// Gets or sets the options used to read/write the records.
        /// </summary>
        public CustomOptions Options
        {
            get { return options; }
            set { options = value ?? new CustomOptions(); }
        }

        /// <summary>
        /// Extracts a single record from the embedded data.
        /// </summary>
        /// <param name="value">The value containing the embedded data.</param>
        /// <returns>
        /// An object array containing the values read from the embedded data -or- null if there is no embedded data.
        /// </returns>
        public override object Parse(string value)
        {
            if (Preprocessor != null)
            {
                value = Preprocessor(value);
            }
            if (NullHandler.IsNullRepresentation(value))
            {
                return null;
            }

            StringReader stringReader = new StringReader(value);
            CustomReader reader = new CustomReader(stringReader, schema, options);
            if (reader.Read())
            {
                return reader.GetValues();
            }
            return null;
        }

        /// <summary>
        /// Formats the given object array into an embedded record.
        /// </summary>
        /// <param name="value">The object array containing the values of the embedded record.</param>
        /// <returns>A formatted string containing the embedded data.</returns>
        public override string Format(object value)
        {
            object[] values = value as object[];
            if (values == null)
            {
                return NullHandler.GetNullRepresentation();
            }
            StringWriter writer = new StringWriter();
            CustomRecordWriter recordWriter = new CustomRecordWriter(writer, schema, options);
            recordWriter.WriteRecord(values);
            return writer.ToString();
        }
    }
}
