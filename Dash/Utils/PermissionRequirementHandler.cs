using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Dash.Utils
{
    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // @todo add support for ParentActionAttribute
            var mvcContext = context.Resource as AuthorizationFilterContext;
            var requiredClaim = $"{mvcContext.RouteData.Values["Controller"]}.{mvcContext.RouteData.Values["Action"]}".ToLower();
            if (context.User.HasClaim(x => x.Type == ClaimTypes.Role && x.Value.ToLower() == requiredClaim))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}