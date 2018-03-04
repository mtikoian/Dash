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
        /// Check if a user if currently logged in.
        /// </summary>
        /// <returns>Returns true if a user is logged in, else false.</returns>
        public bool IsLoggedIn
        {
            get
            {
                return !UserName.IsEmpty();
            }
        }

        /// <summary>
        /// Get the two character language code for the current thread.
        /// </summary>
        public string LanguageCode
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            }
        }

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
        /// Get the currently logged in username.
        /// </summary>
        /// <returns>Returns username if logged in, else empty string.</returns>
        public string UserName
        {
            get
            {
                return HttpContext.Current.User != null && HttpContext.Current.User.Identity.Name.Length > 0
                    ? HttpContext.Current.User.Identity.Name
                    : "";
            }
        }
        

        /// <summary>
        /// Check if the user has context sensitive help enabled.
        /// </summary>
        /// <returns>Returns true if the user has help enabled, else false.</returns>
        public bool WantsHelp
        {
            get
            {
                if (HttpContext.Current.Session["ContextHelp"] == null)
                {
                    return false;
                }
                return (bool)HttpContext.Current.Session["ContextHelp"];
            }
        }

        /// <summary>
        /// Check the user has permissions to view the chart.
        /// </summary>
        /// <param name="report">Chart to check access for.</param>
        /// <returns>Returns true if the user has access to view, else false.</returns>
        public bool CanViewChart(Chart chart)
        {
            return ChartCheckUserAccess(chart);
        }

        /// <summary>
        /// Check the user has permissions to view the report.
        /// </summary>
        /// <param name="report">Report to check access for.</param>
        /// <returns>Returns true if the user has access to view, else false.</returns>
        public bool CanViewReport(Report report)
        {
            return ReportCheckUserAccess(report);
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

        /// <summary>
        /// Check the user has permissions to access the requested dataset.
        /// </summary>
        /// <param name="dataSet">Dataset object to check permissions for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool HasDatasetAccess(Dataset dataset)
        {
            return HasDatasetAccess(dataset.Id);
        }

        /// <summary>
        /// Check the user has permissions to access the requested dataset.
        /// </summary>
        /// <param name="dataSet">DatasetId to check permissions for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool HasDatasetAccess(int datasetId)
        {
            var myUser = User;
            if (myUser == null)
            {
                return false;
            }
            return myUser.Id == DbContext.Query<int>("UserHasDatasetAccess", new { UserId = myUser.Id, DatasetId = datasetId }).FirstOrDefault();
        }

        /// <summary>
        /// Enable/disable help for the session.
        /// </summary>
        /// <param name="status">Status of help.</param>
        public void ToggleContextHelp(bool? status = null)
        {
            if (HttpContext.Current.Session != null)
            {
                if (status.HasValue)
                {
                    HttpContext.Current.Session["ContextHelp"] = status;
                }
                else
                {
                    HttpContext.Current.Session["ContextHelp"] = HttpContext.Current.Session["ContextHelp"] == null ? true : !(bool)HttpContext.Current.Session["ContextHelp"];
                }
            }
        }

        /// <summary>
        /// Check if a user can view a chart.
        /// </summary>
        /// <param name="report">Chart to check access for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        private bool ChartCheckUserAccess(Chart chart)
        {
            var myUser = User;
            if (myUser == null)
            {
                return false;
            }
            if (chart.OwnerId == myUser.Id)
            {
                return true;
            }
            var res = DbContext.Query<bool>("ChartCheckUserAccess", new { UserId = myUser.Id, ChartId = chart.Id });
            return res?.Any() == true && res.First();
        }

        /// <summary>
        /// Check if a user can view a report.
        /// </summary>
        /// <param name="report">Report to check access for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        private bool ReportCheckUserAccess(Report report)
        {
            var myUser = User;
            if (myUser == null)
            {
                return false;
            }
            if (report.OwnerId == myUser.Id)
            {
                return true;
            }
            var res = DbContext.Query<bool>("ReportCheckUserAccess", new { UserId = myUser.Id, ReportId = report.Id });
            return res?.Any() == true && res.First();
        }
    }
}