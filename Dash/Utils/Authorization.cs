using Dash.Models;
using Dash.I18n;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace Dash
{
    /// <summary>
    /// Authorization is a static class for interacting with the session and authorizing users.
    /// </summary>
    public class Authorization
    {
        /// <summary>
        /// Get the currently logged in user.
        /// </summary>
        /// <returns>Returns a user object if one can be found, else null.</returns>
        public User User
        {
            get
            {
                if (HttpContext.Current.Session == null)
                {
                    return null;
                }

                User user = null;

                // check if we have a user in the session
                if (HttpContext.Current.Session["User"] != null)
                {
                    user = (User)HttpContext.Current.Session["User"];
                    // make sure the session user has the correct name and ip - just in case
                    if (!String.Equals(user.UID, UserName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        user = null;
                        HttpContext.Current.Session["User"] = null;
                    }
                }

                // no user in the session so look it up by username
                if (user == null)
                {
                    var uid = UserName;
                    if (uid.Length > 0)
                    {
                        user = Willow.GetAll<User>(new { UID = uid, IsActive = true }).FirstOrDefault();
                        if (user != null)
                        {
                            var cultureInfo = new CultureInfo(user.LanguageCode);
                            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                        }
                    }
                }

                return user;
            }
        }
        
        /// <summary>
        /// Check the user has permissions to access the requested controller and action.
        /// </summary>
        /// <param name="controller">Controller name to check for.</param>
        /// <param name="action">Action name to check for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool HasAccess(string controller, string action)
        {
            var myUser = User;
            if (myUser == null)
            {
                return false;
            }

            if (HttpContext.Current.Session["Permissions"] == null)
            {
                var permissions = Willow.GetAll<Permission>(new { UserId = myUser.Id });
                if (permissions.Any())
                {
                    HttpContext.Current.Session["Permissions"] = permissions.Select(p => p.ControllerName.ToLower() + "|" + p.ActionName.ToLower()).ToList();
                }
                else
                {
                    HttpContext.Current.Session["Permissions"] = new List<string>();
                }
            }

            if (HttpContext.Current.Session["Permissions"] != null)
            {
                return ((List<string>)HttpContext.Current.Session["Permissions"]).Contains(controller.ToLower() + "|" + action.ToLower());
            }

            return false;
        }
    }
}