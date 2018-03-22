using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    /// <summary>
    /// CopyReport is used to copy a report.
    /// </summary>
    public class CopyReport : BaseModel, IValidatableObject
    {
        private Report _Report;

        private IHttpContextAccessor HttpContextAccessor;

        public CopyReport(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        public Report NewReport { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); } }

        /// <summary>
        /// Save the report.
        /// </summary>
        public void Save()
        {
            NewReport = Report.Copy(Prompt);
            DbContext.Save(NewReport);
        }

        /// <summary>
        /// Validate object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Report == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(HttpContextAccessor.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanAccessDataset(Report.DatasetId))
            {
                yield return new ValidationResult(I18n.Reports.ErrorReportDatasetAccess);
            }
        }
    }
}
