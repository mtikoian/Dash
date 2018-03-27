using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
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
