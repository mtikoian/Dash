using Dash.I18n;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class CreateReport : IValidatableObject
    {
        [Display(Name = "Dataset", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength"), StringLength(250)]
        public string Name { get; set; }

        /// <summary>
        /// Validate report object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Authorization.HasDatasetAccess(DatasetId))
            {
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
            }
        }
    }
}