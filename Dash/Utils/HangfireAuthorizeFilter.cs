using System.Security.Claims;
using Hangfire.Dashboard;

namespace Dash.Utils
{
    public class HangfireAuthorizeFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpcontext = context.GetHttpContext();
            return httpcontext.User.Identity.IsAuthenticated && httpcontext.User.HasClaim(x => x.Type == ClaimTypes.Role && x.Value.ToLower() == "hangfire.dashboard");
        }
    }
}
