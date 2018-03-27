using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// Column of a report.
    /// </summary>
    public class ReportColumn : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        public string SortDirection { get; set; }

        public int SortOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public decimal Width { get; set; }
    }
}
