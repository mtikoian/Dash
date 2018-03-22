using System.ComponentModel.DataAnnotations;
using Jil;

namespace Dash.Models
{
    /// <summary>
    /// DatasetColumn is a table column in a dataset.
    /// </summary>
    public class DatasetColumn : BaseModel
    {
        private DataType _DataType;

        [Ignore, JilDirective(true)]
        public string Alias { get { return $"column{Id}"; } }

        [StringLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public string ColumnName { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int DatasetId { get; set; }

        [JilDirective(true)]
        public DataType DataType
        {
            get { return _DataType ?? (_DataType = DbContext.Get<DataType>(DataTypeId)); }
            set { _DataType = value; }
        }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DataTypeId { get; set; }

        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Derived { get; set; }

        [Ignore]
        public int DisplayOrder { get; set; }

        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string FilterQuery { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int FilterTypeId { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsBinary { get { return DataType.IsBinary; } }

        [Ignore, JilDirective(true)]
        public bool IsBool { get { return DataType.IsBool; } }

        [Ignore, JilDirective(true)]
        public bool IsCurrency { get { return DataType.IsCurrency; } }

        [Ignore, JilDirective(true)]
        public bool IsDateTime { get { return DataType.IsDateTime; } }

        [Ignore, JilDirective(true)]
        public bool IsDecimal { get { return DataType.IsDecimal; } }

        [Ignore, JilDirective(true)]
        public bool IsInteger { get { return DataType.IsInteger; } }

        public bool IsParam { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsSelect { get { return FilterTypeId == 3; } }

        [Ignore, JilDirective(true)]
        public bool IsText { get { return DataType.IsText; } }

        [StringLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Link { get; set; }

        [Ignore, JilDirective(true)]
        public int ReportColumnId { get; set; }

        /// <summary>
        /// Get the table from the full column name.
        /// </summary>
        /// <returns>Returns database table name.</returns>
        [Ignore, JilDirective(true)]
        public string Table
        {
            get
            {
                var loc = ColumnName.LastIndexOf(".");
                return loc > -1 ? ColumnName.Substring(0, loc) : "";
            }
        }

        [Ignore, JilDirective(true)]
        public string TableDataType
        {
            get
            {
                if ((FilterTypes)FilterTypeId == FilterTypes.Select)
                {
                    return "string";
                }
                if (IsCurrency)
                {
                    return "currency";
                }
                if (IsInteger || IsDecimal)
                {
                    return "int";
                }
                if (IsDateTime)
                {
                    return "date";
                }
                return "string";
            }
        }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [Ignore, JilDirective(true)]
        public int Width { get; set; }

        /// <summary>
        /// Build the SQL snippet for a single column.
        /// </summary>
        /// <param name="includeAlias">Add in the alias with the SQL snippet.</param>
        /// <returns>Return the SQL for the column.</returns>
        public string BuildSql(bool includeAlias = true, int aggregatorId = 0)
        {
            var columnSql = Derived.IsEmpty() ? ColumnName : Derived;
            if (columnSql.Length == 0)
            {
                return columnSql;
            }

            columnSql = $"({columnSql})";
            if (aggregatorId > 0)
            {
                if (IsBinary && DataType.Name.ToLower() != "binary")
                {
                    // handling for guids
                    columnSql = $"(CAST({columnSql} AS NVARCHAR(1000)))";
                }
                columnSql = $"({(IsText || IsDateTime ? "MAX" : ((Aggregators)aggregatorId).ToString().ToUpper())}{columnSql})";
            }
            return includeAlias ? $"{columnSql} AS {Alias}" : columnSql;
        }
    }
}
