namespace Dash.Models
{
    /// <summary>
    /// Column is used for displaying widgets/reports.
    /// </summary>
    public class Column : BaseModel
    {
        private DataType _DataType;

        /// <summary>
        /// Get the column alias.
        /// </summary>
        /// <returns>Returns the column alias.</returns>
        public string Alias { get { return "column" + ColumnId; } }

        public int ColumnId { get; set; }
        public DataType DataType { get { return _DataType ?? (_DataType = DbContext.Get<DataType>(DataTypeId)); } }
        public int DataTypeId { get; set; }
        public int DisplayOrder { get; set; }

        public bool IsBool { get { return DataType.IsBool; } }
        public bool IsCurrency { get { return DataType.IsCurrency; } }
        public bool IsDateTime { get { return DataType.IsDateTime; } }
        public bool IsDecimal { get { return DataType.IsDecimal; } }
        public bool IsInteger { get { return DataType.IsInteger; } }
        public bool IsText { get { return DataType.IsText; } }
        public string Link { get; set; }
        public string SortDirection { get; set; }
        public int SortOrder { get; set; }

        public TableDataType TableDataType
        {
            get
            {
                if (IsCurrency)
                {
                    return TableDataType.Currency;
                }
                if (IsInteger || IsDecimal)
                {
                    return TableDataType.Int;
                }
                if (IsDateTime)
                {
                    return TableDataType.Date;
                }
                return TableDataType.String;
            }
        }

        public string Title { get; set; }
        public int Width { get; set; }
    }
}
