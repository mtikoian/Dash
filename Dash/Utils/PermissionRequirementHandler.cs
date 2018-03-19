using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Dash.Utils
{
    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var mvcContext = context.Resource as AuthorizationFilterContext;
            var requiredClaim = $"{mvcContext.RouteData.Values["Controller"]}.{mvcContext.RouteData.Values["Action"]}".ToLower();
            var controllerActionDescriptor = mvcContext.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                var actionAttributes = controllerActionDescriptor.MethodInfo.GetCustomAttributes(inherit: true).Cast<Attribute>();
                var parentAction = actionAttributes.FirstOrDefault(x => x.GetType() == typeof(ParentActionAttribute));
                if (parentAction != null)
                {
                    requiredClaim = $"{mvcContext.RouteData.Values["Controller"]}.{((ParentActionAttribute)parentAction).ActionName}".ToLower();
                }
            }

            if (context.User.HasClaim(x => x.Type == ClaimTypes.Role && x.Value.ToLower() == requiredClaim))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}