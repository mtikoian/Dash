using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class RolePermission : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int PermissionId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }
    }
}
