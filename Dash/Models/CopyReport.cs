using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// CopyReport is used to copy a report.
    /// </summary>
    public class CopyReport : BaseModel, IValidatableObject
    {
        private Report _Report;

        [Required(ErrorMessageResourceType = typeof(I18n.Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public Report Report { get { return _Report ?? (_Report = Report.FromId(Id)); } }
        public Report NewReport { get; set; }

        /// <summary>
        /// Save the report.
        /// </summary>
        public void Save()
        {
            NewReport = Report.Copy(Prompt);
            NewReport.Save();
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
            if (!Authorization.HasDatasetAccess(Report.DatasetId))
            {
                yield return new ValidationResult(I18n.Reports.ErrorReportDatasetAccess);
            }
        }
    }
}