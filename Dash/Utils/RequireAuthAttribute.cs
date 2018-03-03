using Dash.I18n;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dash
{
    public class ParentActionAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Specifies that access to the action is dependent on having access to the parent action.
        /// </summary>
        /// <param name="action">Parent action.</param>
        public ParentActionAttribute(string action)
        {
            Action = action;
        }

        public string Action { get; set; }
    }

    public class RedirectValues
    {
        public string Action { get; set; }
        public string Controller { get; set; }
        public object RouteValues { get; set; }
    }

    /// <summary>
    /// Runs before action to check user permissions.
    /// </summary>
    public class RequireAuthAttribute : ActionFilterAttribute, IActionFilter
    {
        /// <summary>
        /// Check user permissions.
        /// </summary>
        /// <param name="filterContext">Current filter context.</param>
        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            var action = filterContext.ActionDescriptor.ActionName;

            if (!Authorization.IsLoggedIn)
            {
                filterContext.Controller.TempData["Error"] = Core.ErrorNotLoggedIn;
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn" }));
            }
            else if (!filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(SkipAuthenticationAttribute), false).Any())
            {
                var parentAttr = filterContext.ActionDescriptor.GetCustomAttributes(typeof(ParentActionAttribute), false);
                if (parentAttr.Any())
                {
                    action = ((ParentActionAttribute)parentAttr.First()).Action;
                }

                if (!Authorization.HasAccess(controller, action))
                {
                    filterContext.Controller.TempData["Error"] = Core.ErrorAuthorization;
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Dashboard", action = "Index" }));
                }
            }

            OnActionExecuting(filterContext);
        }
    }

    public class SkipAuthenticationAttribute : ActionFilterAttribute { }
}