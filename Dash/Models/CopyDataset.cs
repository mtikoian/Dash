using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// CopyDataset is used to copy a dataset.
    /// </summary>
    public class CopyDataset : BaseModel, IValidatableObject
    {
        private Dataset _Dataset;

        [Required(ErrorMessageResourceType = typeof(I18n.Datasets), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public Dataset Dataset { get { return _Dataset ?? (_Dataset = DbContext.Get<Dataset>(Id)); } }

        /// <summary>
        /// Save the dataset.
        /// </summary>
        public void Save()
        {
            Dataset.Copy(Prompt).Save();
        }

        /// <summary>
        /// Validate object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Dataset == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
            if (!Dataset.IsUniqueName(Prompt, 0))
            {
                yield return new ValidationResult(I18n.Datasets.ErrorDuplicateName);
            }
        }
    }
}