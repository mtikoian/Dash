using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// RolePermission is many to many link between roles and permissions.
    /// </summary>
    public class RolePermission : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int PermissionId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }
    }
}