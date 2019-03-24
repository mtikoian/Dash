using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class ReportShare : Share
    {
        Report _Report;

        public ReportShare() { }

        public ReportShare(IDbContext dbContext) => DbContext = dbContext;

        public ReportShare(IDbContext dbContext, int reportId)
        {
            DbContext = dbContext;
            ReportId = reportId;
        }

        [BindNever, ValidateNever]
        public Report Report => _Report ?? (_Report = DbContext.Get<Report>(ReportId));

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName => Report?.Name;
    }
}
