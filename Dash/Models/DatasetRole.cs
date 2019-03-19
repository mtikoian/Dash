using System;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class DatasetRole : BaseModel, IEquatable<DatasetRole>
    {
        [Required]
        public int DatasetId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public bool Equals(DatasetRole other) => other.DatasetId == DatasetId && other.RoleId == RoleId;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals(obj as DatasetRole);
        }

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
