using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Configuration;
using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class ReportData : BaseModel, IValidatableObject
    {
        IHttpContextAccessor _HttpContextAccessor;
        Report _Report;

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        public int? Items { get; set; }

        [BindNever, ValidateNever]
        public Report Report => _Report ?? (_Report = DbContext.Get<Report>(Id));

        public bool Save { get; set; }
        public List<TableSorting> Sort { get; set; }
        public int? StartItem { get; set; }

        [BindNever, ValidateNever]
        public int TotalItems => Items ?? Report?.RowLimit ?? 0;

        public ReportResult GetResult() => Report.GetData(AppConfig, StartItem ?? 0, TotalItems, false);

        public void Update()
        {
            if (Save)
                Report.DataUpdate(TotalItems, Sort);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            AppConfig = (IAppConfiguration)validationContext.GetService(typeof(IAppConfiguration));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));

            if (Report == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            else
            {
                var user = DbContext.Get<User>(_HttpContextAccessor.HttpContext.User.UserId());
                if (!user.CanViewReport(Report))
                    yield return new ValidationResult(Reports.ErrorPermissionDenied);
                if ((Report.Dataset?.DatasetColumn?.Count ?? 0) == 0)
                    yield return new ValidationResult(Reports.ErrorNoColumnsSelected);
            }
        }
    }
}
