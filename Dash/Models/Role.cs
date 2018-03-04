using Jil;
using Dash.I18n;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// Role is a single authorization role.
    /// </summary>
    [HasMany(typeof(RolePermission))]
    [HasMany(typeof(UserRole))]
    public class Role : BaseModel, IValidatableObject
    {
        private List<User> _AllUsers;
        private Dictionary<string, List<Permission>> _ControllerPermissions;
        private List<RolePermission> _RolePermission;
        private List<UserRole> _UserRole;

        /// <summary>
        /// Make a keyed list of all users.
        /// </summary>
        /// <returns>Dictionary of user keyed by userID.</returns>
        [JilDirective(true)]
        public List<User> AllUsers
        {
            get { return _AllUsers ?? (_AllUsers = DbContext.GetAll<User>().ToList()); }
        }
        /// <summary>
        /// Make a keyed list of all permissions.
        /// </summary>
        /// <returns>Dictionary of permissions keyed by permissionID.</returns>
        [JilDirective(true)]
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

        [Display(Name = "Name", ResourceType = typeof(I18n.Roles))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Ignore]
        public List<int> PermissionIds { get; set; }

        [JilDirective(true)]
        public List<RolePermission> RolePermission
        {
            get { return _RolePermission ?? (_RolePermission = DbContext.GetAll<RolePermission>(new { RoleId = Id }).ToList()); }
            set { _RolePermission = value; }
        }

        [Ignore]
        public List<int> UserIds { get; set; }

        [JilDirective(true)]
        public List<UserRole> UserRole
        {
            get { return _UserRole ?? (_UserRole = DbContext.GetAll<UserRole>(new { RoleId = Id }).ToList()); }
            set { _UserRole = value; }
        }

        /// <summary>
        /// Make sure no other roles have the same name.
        /// </summary>
        /// <param name="name">Name to check for.</param>
        /// <param name="id">ID of current role.</param>
        /// <returns>True</returns>
        public bool IsUniqueName(string name, int id)
        {
            return !DbContext.GetAll<Role>(new { Name = name }).Any(x => x.Id != id);
        }

        /// <summary>
        /// Copy a role.
        /// </summary>
        /// <param name="name">New role name.</param>
        public Role Copy(string name = null)
        {
            var newRole = this.Clone();
            newRole.Id = 0;
            newRole.Name = name.IsEmpty() ? String.Format(I18n.Core.CopyOf, Name) : name;
            newRole.RolePermission = (RolePermission ?? DbContext.GetAll<RolePermission>(new { RoleId = Id }))?.Select(x => new RolePermission { PermissionId = x.PermissionId }).ToList();
            newRole.UserRole = (UserRole ?? DbContext.GetAll<UserRole>(new { RoleId = Id }))?.Select(x => new UserRole { UserId = x.UserId }).ToList();
            return newRole;
        }

        /// <summary>
        /// Save a role, including updating the rolePermissions and userRoles.
        /// </summary>
        public void Save()
        {
            // set the new permissions
            if (PermissionIds?.Any() == true)
            {
                // make a list of all role permissions
                var keyedRolePermissions = DbContext.GetAll<RolePermission>(new { RoleId = Id }).ToDictionary(x => x.PermissionId, x => x);
                RolePermission = PermissionIds?.Where(x => x > 0)
                    .Select(id => keyedRolePermissions.ContainsKey(id) ? keyedRolePermissions[id] : new RolePermission { PermissionId = id, RoleId = Id }).ToList()
                    ?? new List<RolePermission>();
            }

            // set the new users
            if (UserIds?.Any() == true)
            {
                // make a list of all user roles
                var keyedUserRoles = DbContext.GetAll<UserRole>(new { RoleId = Id }).ToDictionary(x => x.UserId, x => x);
                UserRole = UserIds?.Where(x => x > 0).Select(id => keyedUserRoles.ContainsKey(id) ? keyedUserRoles[id] : new UserRole { UserId = id, RoleId = Id }).ToList()
                    ?? new List<UserRole>();
            }

            // try saving
            DbContext.Save(this);
        }

        /// <summary>
        /// Validate role object. Check that name is unique.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsUniqueName(Name, Id))
            {
                yield return new ValidationResult(I18n.Roles.ErrorDuplicateName, new[] { "Name" });
            }
        }
    }
}