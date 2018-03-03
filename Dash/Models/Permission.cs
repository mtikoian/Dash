using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// Permission is a combination of action and controller, used for user authorization. AuthActionFilter checks permissions.
    /// </summary>
    public class Permission : BaseModel, IValidatableObject
    {
        [Display(Name = "Action", ResourceType = typeof(I18n.Permissions))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string ActionName { get; set; }

        [Display(Name = "Controller", ResourceType = typeof(I18n.Permissions))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string ControllerName { get; set; }

        [Ignore]
        public string FullName { get { return ControllerName?.Trim() + "." + ActionName?.Trim(); } }

        /// <summary>
        /// Load a permission by ID.
        /// </summary>
        /// <param name="id">Permission ID</param>
        /// <returns>Permission object or null.</returns>
        public static Permission FromId(int id)
        {
            return DbContext.Get<Permission>(id);
        }

        /// <summary>
        /// Validate permission object. Check that controller/action is unique.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DbContext.GetAll<Permission>().Any(x => x.ControllerName.ToUpper() == ControllerName.ToUpper() && x.ActionName.ToUpper() == ActionName.ToUpper() && x.Id != Id))
            {
                yield return new ValidationResult(I18n.Permissions.ErrorDuplicateName, new[] { "ControllerName" });
            }
        }
    }
}