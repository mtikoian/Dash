using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Dash.Controllers
{
    /// <summary>
    /// Controller for displaying errors.
    /// </summary>
    public class ErrorController : BaseController
    {
        public ErrorController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Displays error message.
        /// </summary>
        /// <returns>Returns the dashboard page.</returns>
        [SkipActivityLog]
        public IActionResult Index()
        {
            return View("Error");
        }

        /// <summary>
        /// Log javascript errors to elmah.
        /// </summary>
        /// <param name="message"></param>
        [SkipActivityLog]
        public void LogJavascriptError(string message)
        {
            new Exception(message).Log(HttpContextAccessor);
        }
    }

    /// <summary>
    /// For saving front-end errors to ELMAH.
    /// </summary>
    [Serializable]
    public class JavaScriptException : Exception
    {
        public JavaScriptException(string message) : base(message)
        {
        }
    }
}