using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class CopyRole : BaseModel, IValidatableObject
    {
        private Role _Role;

        [Required(ErrorMessageResourceType = typeof(Roles), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        [BindNever, ValidateNever]
        public Role Role { get { return _Role ?? (_Role = DbContext.Get<Role>(Id)); } }

        public void Save()
        {
            Role.Copy(Prompt).Save();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Role == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            if (!Role.IsUniqueName(Prompt, 0))
            {
                yield return new ValidationResult(Roles.ErrorDuplicateName);
            }
        }
    }
}
