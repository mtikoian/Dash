using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Jil;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    [HasMany(typeof(RolePermission))]
    [HasMany(typeof(UserRole))]
    public class Role : BaseModel, IValidatableObject
    {
        private List<User> _AllUsers;
        private Dictionary<string, List<Permission>> _ControllerPermissions;
        private List<RolePermission> _RolePermission;
        private List<UserRole> _UserRole;

        public Role()
        {
        }

        public Role(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [JilDirective(true)]
        [BindNever, ValidateNever]
        public List<User> AllUsers
        {
            get { return _AllUsers ?? (_AllUsers = DbContext.GetAll<User>().ToList()); }
        }

        [JilDirective(true)]
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

        [Ignore]
        public List<int> PermissionIds { get; set; }

        [JilDirective(true)]
        [BindNever, ValidateNever]
        public List<RolePermission> RolePermission
        {
            get { return _RolePermission ?? (_RolePermission = DbContext.GetAll<RolePermission>(new { RoleId = Id }).ToList()); }
            set { _RolePermission = value; }
        }

        [Ignore]
        public List<int> UserIds { get; set; }

        [JilDirective(true)]
        [BindNever, ValidateNever]
        public List<UserRole> UserRole
        {
            get { return _UserRole ?? (_UserRole = DbContext.GetAll<UserRole>(new { RoleId = Id }).ToList()); }
            set { _UserRole = value; }
        }

        public Role Copy(string name = null)
        {
            var newRole = this.Clone();
            newRole.Id = 0;
            newRole.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;
            newRole.RolePermission = (RolePermission ?? DbContext.GetAll<RolePermission>(new { RoleId = Id }))?.Select(x => new RolePermission { PermissionId = x.PermissionId }).ToList();
            newRole.UserRole = (UserRole ?? DbContext.GetAll<UserRole>(new { RoleId = Id }))?.Select(x => new UserRole { UserId = x.UserId }).ToList();
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
            var keyedUserRoles = DbContext.GetAll<UserRole>(new { RoleId = Id }).ToDictionary(x => x.UserId, x => x);
            UserRole = UserIds?.Where(x => x > 0).Select(id => keyedUserRoles.ContainsKey(id) ? keyedUserRoles[id] : new UserRole { UserId = id, RoleId = Id }).ToList()
                ?? new List<UserRole>();
            DbContext.Save(this, forceSaveNulls: true);
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
