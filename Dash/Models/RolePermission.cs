using System;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class RolePermission : BaseModel, IEquatable<RolePermission>
    {
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int PermissionId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }

        public bool Equals(RolePermission other)
        {
            return other.PermissionId == PermissionId && other.RoleId == RoleId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as RolePermission);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
