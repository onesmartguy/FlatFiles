using FlatFiles.Resources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FlatFiles.TypeMapping
{

    /// <summary>
    /// Represents the mapping from a type property to an object.
    /// </summary>
    public interface ICustomComplexPropertyMapping
    {
        /// <summary>
        /// Sets the name of the column in the input or output file.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The property mapping for further configuration.</returns>
        ICustomComplexPropertyMapping ColumnName(string name);

        /// <summary>
        /// Sets the options to use when reading/writing the complex type.
        /// </summary>
        /// <param name="options">The options to use.</param>
        /// <returns>The property mapping for further configuration.</returns>
        ICustomComplexPropertyMapping WithOptions(CustomOptions options);

        /// <summary>
        /// Sets the value to treat as null.
        /// </summary>
        /// <param name="value">The value to treat as null.</param>
        /// <returns>The property mapping for further configuration.</returns>
        ICustomComplexPropertyMapping NullValue(string value);

        /// <summary>
        /// Sets a custom handler for nulls.
        /// </summary>
        /// <param name="handler">The handler to use to recognize nulls.</param>
        /// <returns>The property mapping for further configuration.</returns>
        /// <remarks>Setting the handler to null with use the default handler.</remarks>
        ICustomComplexPropertyMapping NullHandler(INullHandler handler);

        /// <summary>
        /// Sets a function to preprocess in the input before parsing it.
        /// </summary>
        /// <param name="preprocessor">A preprocessor function.</param>
        /// <returns>The property mapping for further configuration.</returns>
        ICustomComplexPropertyMapping Preprocessor(Func<string, string> preprocessor);
    }

    public class CustomComplexPropertyMapping<TEntity> : ICustomComplexPropertyMapping, IPropertyMapping
    {
        private readonly ICustomTypeMapper<TEntity> mapper;
        private readonly PropertyInfo property;
        private string columnName;
        private CustomOptions options;
        private INullHandler nullHandler;
        private Func<string, string> preprocessor;

        public CustomComplexPropertyMapping(ICustomTypeMapper<TEntity> mapper, PropertyInfo property)
        {
            this.mapper = mapper;
            this.property = property;
            this.columnName = property.Name;
        }

        public IColumnDefinition ColumnDefinition
        {
            get
            {
                CustomSchema schema = mapper.GetSchema();
                CustomComplexColumn column = new CustomComplexColumn(columnName, schema);
                column.Options = options;
                column.NullHandler = nullHandler;
                column.Preprocessor = preprocessor;

                var recordMapper = (IRecordMapper<TEntity>)mapper;
                return new ComplexMapperColumn<TEntity>(column, recordMapper);
            }
        }

        public PropertyInfo Property
        {
            get { return property; }
        }

        public ICustomComplexPropertyMapping ColumnName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(SharedResources.BlankColumnName);
            }
            this.columnName = name;
            return this;
        }

        public ICustomComplexPropertyMapping WithOptions(CustomOptions options)
        {
            this.options = options;
            return this;
        }

        public ICustomComplexPropertyMapping NullHandler(INullHandler handler)
        {
            this.nullHandler = handler;
            return this;
        }

        public ICustomComplexPropertyMapping NullValue(string value)
        {
            this.nullHandler = new ConstantNullHandler(value);
            return this;
        }

        public ICustomComplexPropertyMapping Preprocessor(Func<string, string> preprocessor)
        {
            this.preprocessor = preprocessor;
            return this;
        }
    }
}
