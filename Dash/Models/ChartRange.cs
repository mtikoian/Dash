using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class ChartRange : BaseModel
    {
        public int AggregatorId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        public string Color { get; set; }
        public int DateIntervalId { get; set; }
        public int DisplayOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int XAxisColumnId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int YAxisColumnId { get; set; }
    }
}
