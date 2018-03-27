using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class RolePermission : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int PermissionId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }
    }
}
