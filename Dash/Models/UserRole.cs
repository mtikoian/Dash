using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// UserRole is the link between a role and a user.
    /// </summary>
    public class UserRole : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }

        [Ignore]
        public string RoleName { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int UserId { get; set; }
    }
}
