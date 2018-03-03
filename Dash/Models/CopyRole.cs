using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// CopyRole is used to copy a role.
    /// </summary>
    public class CopyRole : BaseModel, IValidatableObject
    {
        private Role _Role;

        [Required(ErrorMessageResourceType = typeof(I18n.Roles), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public Role Role
        {
            get {
                return _Role ?? (_Role = Role.FromId(Id));
            }
        }

        /// <summary>
        /// Save the role.
        /// </summary>
        public void Save()
        {
            Role.Copy(Prompt).Save();
        }

        /// <summary>
        /// Validate object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
            if (!Role.IsUniqueName(Prompt, 0))
            {
                yield return new ValidationResult(I18n.Roles.ErrorDuplicateName);
            }
        }
    }
}