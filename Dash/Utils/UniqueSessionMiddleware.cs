using System;
using System.Linq;
using System.Threading.Tasks;
using Dash.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dash
{
    internal class UniqueSessionMiddleware
    {
        readonly RequestDelegate Next;
        const string SessionInvalidPath = "/Account/SessionInvalid";

        public UniqueSessionMiddleware(RequestDelegate next) => Next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task Invoke(HttpContext httpContext, IDbContext dbContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            if (!httpContext.Request.Path.StartsWithSegments(SessionInvalidPath) && httpContext.User.Identity.IsAuthenticated)
            {
                var membership = dbContext.GetAll<UserMembership>(new { UserName = httpContext.User.Identity.Name }).FirstOrDefault();
                if (membership?.SessionId != httpContext.User.SessionId())
                {
                    await AuthenticationHttpContextExtensions.SignOutAsync(httpContext);
                    httpContext.Response.Redirect(SessionInvalidPath);
                }
            }
            await Next(httpContext);
        }
    }
}
