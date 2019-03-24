using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class ReportFilterCriteria : BaseModel
    {
        public ReportFilterCriteria() { }

        public ReportFilterCriteria(IDbContext dbContext) => DbContext = dbContext;

        public ReportFilterCriteria(IDbContext dbContext, int reportFilterId, string value, int? userId)
        {
            DbContext = dbContext;
            ReportFilterId = reportFilterId;
            Value = value;
            RequestUserId = userId;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportFilterId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public string Value { get; set; }
    }
}
