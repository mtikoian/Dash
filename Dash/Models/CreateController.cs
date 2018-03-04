using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// Controller is used to create multiple permissions for the same controller.
    /// </summary>
    public class CreateController : BaseModel, IValidatableObject
    {
        public enum Actions { Index, Create, Edit, Delete, Details };

        public IEnumerable<string> ActionList { get { return Enum.GetNames(typeof(Actions)); } }

        public IEnumerable<string> ControllerActions { get; set; }

        [Display(Name = "Controller", ResourceType = typeof(I18n.Permissions))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string ControllerName { get; set; }

        /// <summary>
        /// Save the permissions for the controller.
        /// </summary>
        public void Save()
        {
            ControllerActions.Select(x => new Permission() { ActionName = x, ControllerName = ControllerName })
                .Each(x => DbContext.Save(x));
        }

        /// <summary>
        /// Validate controller object. Check that controller has valid actions.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ControllerActions?.Any() != true)
            {
                yield return new ValidationResult(I18n.Permissions.ErrorSelectAction, new[] { "ControllerActions" });
            }
        }
    }
}