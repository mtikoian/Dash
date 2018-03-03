using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// Group the data in a report by a column.
    /// </summary>
    public class ReportGroup : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }
    }
}