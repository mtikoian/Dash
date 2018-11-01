using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class Role : BaseModel, IValidatableObject
    {
        private Dictionary<string, List<Permission>> _ControllerPermissions;
        private List<RolePermission> _RolePermission;

        public Role()
        {
        }

        public Role(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [BindNever, ValidateNever]
        public Dictionary<string, List<Permission>> ControllerPermissions
        {
            get
            {
                if (_ControllerPermissions == null)
                {
                    _ControllerPermissions = new Dictionary<string, List<Permission>>();
                    DbContext.GetAll<Permission>().Each(permission => {
                        if (!_ControllerPermissions.ContainsKey(permission.ControllerName))
                        {
                            _ControllerPermissions.Add(permission.ControllerName, new List<Permission>());
                        }
                        _ControllerPermissions[permission.ControllerName].Add(permission);
                    });
                }
                return _ControllerPermissions;
            }
        }

        [Display(Name = "Name", ResourceType = typeof(Roles))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "Permissions", ResourceType = typeof(Roles))]
        [DbIgnore]
        public List<int> PermissionIds { get; set; }

        [BindNever, ValidateNever]
        public List<RolePermission> RolePermission
        {
            get { return _RolePermission ?? (_RolePermission = DbContext.GetAll<RolePermission>(new { RoleId = Id }).ToList()); }
            set { _RolePermission = value; }
        }

        public Role Copy(string name = null)
        {
            var newRole = this.Clone();
            newRole.Id = 0;
            newRole.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;
            newRole.RequestUserId = RequestUserId;
            newRole.PermissionIds = RolePermission?.Select(x => x.PermissionId).ToList();
            return newRole;
        }

        public bool IsUniqueName(string name, int id)
        {
            return !DbContext.GetAll<Role>(new { Name = name }).Any(x => x.Id != id);
        }

        public void Save()
        {
            var keyedRolePermissions = DbContext.GetAll<RolePermission>(new { RoleId = Id }).ToDictionary(x => x.PermissionId, x => x);
            RolePermission = PermissionIds?.Where(x => x > 0)
                .Select(id => keyedRolePermissions.ContainsKey(id) ? keyedRolePermissions[id] : new RolePermission { PermissionId = id, RoleId = Id }).ToList()
                ?? new List<RolePermission>();
            RolePermission.Each(x => x.RequestUserId = RequestUserId);
            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                DbContext.SaveMany(this, RolePermission);
            });
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsUniqueName(Name, Id))
            {
                yield return new ValidationResult(Roles.ErrorDuplicateName, new[] { "Name" });
            }
        }
    }
}
