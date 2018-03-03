using Dash.Models;
using System;
using System.Linq;
using System.Web.Security;

namespace Dash
{
    public class CustomRoleProvider : RoleProvider
    {
        private string applicationName;

        public override string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }

        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            throw new NotImplementedException("AddUsersToRoles not found.");
        }

        public override void CreateRole(string rolename)
        {
            throw new NotImplementedException("CreateRole not found.");
        }

        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException("DeleteRole not found.");
        }

        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            throw new NotImplementedException("FindUsersInRole not found.");
        }

        public override string[] GetAllRoles()
        {
            var res = Willow.GetAll<Role>();
            return res.Select(r => r.Name).ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            var userRoles = Willow.GetAll<UserRole>(new { UID = username });
            if (userRoles.Any())
            {
                return userRoles.Select(r => r.RoleName).ToArray();
            }
            else
            {
                return new string[] { };
            }
        }

        public override string[] GetUsersInRole(string rolename)
        {
            throw new NotImplementedException("GetUsersInRole not found.");
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            string[] roles = GetRolesForUser(username);
            return (roles.ToList().IndexOf(roleName) > -1);
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            throw new NotImplementedException("RemoveUsersFromRoles not found.");
        }

        public override bool RoleExists(string roleName)
        {
            string[] roles = GetAllRoles();
            return (roles.ToList().IndexOf(roleName) > -1);
        }
    }
}