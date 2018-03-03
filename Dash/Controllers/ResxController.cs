using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Controllers
{
    public class ResxController : BaseController
    {
        public ResxController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Return the translation reousrces for the user.
        /// </summary>
        /// <returns>Object with all resources strings the user needs.</returns>
        [HttpGet, AllowAnonymous]
        public IActionResult Index()
        {
            return Json(ResX.Build());
        }
    }
}