using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class CopyReport : BaseModel, IValidatableObject
    {
        IHttpContextAccessor _HttpContextAccessor;
        Report _Report;

        [Required(ErrorMessageResourceType = typeof(Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        [BindNever, ValidateNever]
        public Report Report
        {
            get => _Report ?? (_Report = DbContext.Get<Report>(Id));
            set => _Report = value;
        }

        public void Save()
        {
            Report = Report.Copy(Prompt);
            Report.Save(true);
            Id = Report.Id;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));
            if (Report == null)
                yield return new ValidationResult(Core.ErrorInvalidId);
            if (DbContext.Get<User>(_HttpContextAccessor.HttpContext.User.UserId())?.CanAccessDataset(Report.DatasetId) != true)
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
        }
    }
}
