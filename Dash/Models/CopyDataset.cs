using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class CopyDataset : BaseModel, IValidatableObject
    {
        private Dataset _Dataset;

        [BindNever, ValidateNever]
        public Dataset Dataset => _Dataset ?? (_Dataset = DbContext.Get<Dataset>(Id));

        [Required(ErrorMessageResourceType = typeof(Datasets), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public void Save() => Dataset.Copy(Prompt).Save();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Dataset == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            if (!Dataset.IsUniqueName(Prompt, 0))
            {
                yield return new ValidationResult(Datasets.ErrorDuplicateName);
            }
        }
    }
}
