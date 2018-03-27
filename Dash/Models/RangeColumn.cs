namespace Dash.Models
{
    /// <summary>
    /// RangeColumn is a simple model for displaying columns when creating chart ranges.
    /// </summary>
    public class RangeColumn : BaseModel
    {
        public int ColumnId { get; set; }
        public int FilterTypeId { get; set; }
        public int ReportId { get; set; }
        public string Title { get; set; }
    }
}
