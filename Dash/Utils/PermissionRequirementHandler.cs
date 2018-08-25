using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dash.Utils
{
    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var mvcContext = context.Resource as AuthorizationFilterContext;
            var parentActionAttributes = (mvcContext.ActionDescriptor as ControllerActionDescriptor).MethodInfo
                .GetCustomAttributes(typeof(ParentActionAttribute), true).Cast<ParentActionAttribute>().Where(x => !x.Action.IsEmpty());
            if (parentActionAttributes.Any())
            {
                parentActionAttributes.SelectMany(x => x.Action.Split(',')).Where(x => !x.IsEmpty()).Each(action => {
                    if (context.User.IsInRole($"{mvcContext.RouteData.Values["Controller"]}.{action}".ToLower()))
                    {
                        context.Succeed(requirement);
                    }
                });
            }
            else if (context.User.IsInRole($"{mvcContext.RouteData.Values["Controller"]}.{mvcContext.RouteData.Values["Action"]}".ToLower()))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
