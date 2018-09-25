namespace Dash
{
    public enum TableDataType
    {
        String,
        Int,
        Date,
        Currency
    }

    public class TableColumnWidth
    {
        public string Field { get; set; }
        public decimal Width { get; set; }
    }

    public class TableSorting
    {
        public string SortDir { get; set; }
        public string Field { get; set; }
        public int SortOrder { get; set; }
    }
}
