using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Controllers
{
    [Authorize]
    public class ResxController : BaseController
    {
        public ResxController(IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig) :
            base(dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Return the translation reousrces for the user.
        /// </summary>
        /// <returns>Object with all resources strings the user needs.</returns>
        [HttpGet, AllowAnonymous]
        public IActionResult Index()
        {
            return Json(ResX.Build(User));
        }
    }
}