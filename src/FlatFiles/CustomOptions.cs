using System;
using FlatFiles.Resources;

namespace FlatFiles
{
    /// <summary>
    /// Holds configuration settings for the FixedLengthParser class.
    /// </summary>
    public class CustomOptions
    {
        private OverflowTruncationPolicy truncationPolicy;
        private string separator;
        private string recordSeparator;

        /// <summary>
        /// Initializes a new instance of a FixedLengthParserOptions.
        /// </summary>
        public CustomOptions()
        {
            Separator = ",";
            RecordSeparator = Environment.NewLine;
            Quote = '"';
            QuoteBehavior = QuoteBehavior.Default;
            FillCharacter = ' ';
            RecordSeparator = Environment.NewLine;
            truncationPolicy = OverflowTruncationPolicy.TruncateLeading;
        }

        /// <summary>
        /// Gets or sets the character used to buffer values in a column.
        /// </summary>
        public char FillCharacter { get; set; }

        /// <summary>
        /// Gets or sets the string that indicates the end of a record.
        /// </summary>
        public string RecordSeparator { get; set; }

        /// <summary>
        /// Gets or sets the separator used to separate the columns.
        /// </summary>
        public string Separator
        {
            get
            {
                return separator;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SharedResources.EmptySeparator);
                }
                this.separator = value;
            }
        }

   

        /// <summary>
        /// Gets or sets the character used to quote records containing special characters.
        /// </summary>
        public char Quote { get; set; }

        /// <summary>
        /// Gets or sets how FlatFiles will handle quoting values.
        /// </summary>
        public QuoteBehavior QuoteBehavior { get; set; }

        /// <summary>
        /// Gets or sets whether the first record in the source holds header information and should be skipped.
        /// </summary>
        public bool IsFirstRecordHeader { get; set; }

        /// <summary>
        /// Gets or sets a filter to use to skip records prior to record being partitioned.
        /// </summary>
        public Func<string, bool> UnpartitionedRecordFilter { get; set; }

        /// <summary>
        /// Gets or sets a filter to use to skip records after the record is partitioned.
        /// </summary>
        public Func<string[], bool> PartitionedRecordFilter { get; set; }

        /// <summary>
        /// Gets or sets the default overflow truncation policy to use
        /// when a value exceeds the maximum length of its column.
        /// </summary>
        public OverflowTruncationPolicy TruncationPolicy
        {
            get
            {
                return truncationPolicy;
            }
            set
            {
                if (!Enum.IsDefined(typeof(OverflowTruncationPolicy), value))
                {
                    throw new ArgumentException(SharedResources.InvalidTruncationPolicy, "value");
                }
                truncationPolicy = value;
            }
        }

        /// <summary>
        /// Raised when an error occurs while processing a record.
        /// </summary>
        public EventHandler<ProcessingErrorEventArgs> ErrorHandler;

        /// <summary>
        /// Duplicates the options.
        /// </summary>
        /// <returns>The new options.</returns>
        public CustomOptions Clone()
        {
            return (CustomOptions)MemberwiseClone();
        }
    }

}
