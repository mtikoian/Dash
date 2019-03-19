using System;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class UserRole : BaseModel, IEquatable<UserRole>
    {
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RoleId { get; set; }

        [DbIgnore]
        public string RoleName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int UserId { get; set; }

        public bool Equals(UserRole other) => other.UserId == UserId && other.RoleId == RoleId;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals(obj as UserRole);
        }

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
