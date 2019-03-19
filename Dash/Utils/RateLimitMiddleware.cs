using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dash.Utils
{
    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomIpRateLimiting(this IApplicationBuilder builder) => builder.UseMiddleware<IpRateLimitMiddlewareCustom>();
    }

    public class IpRateLimitMiddlewareCustom : IpRateLimitMiddleware
    {
        ILogger _Logger;

        public IpRateLimitMiddlewareCustom(RequestDelegate next, IOptions<IpRateLimitOptions> options, IRateLimitCounterStore counterStore, IIpPolicyStore policyStore, ILogger<IpRateLimitMiddleware> logger, IIpAddressParser ipParser = null) : base(next, options, counterStore, policyStore, logger, ipParser) => _Logger = logger;

        public override void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            _Logger.LogWarning("Request {0}:{1} from IP {2} has been blocked, quota {3}/{4} exceeded by {5}. Blocked by rule {6}, TraceIdentifier {7}.",
                identity.HttpVerb.ToUpper(), identity.Path, identity.ClientIp, rule.Limit, rule.Period, counter.TotalRequests, rule.Endpoint, httpContext.TraceIdentifier);
        }
    }
}
