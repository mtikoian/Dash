using System;
using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Controllers
{
    /// <summary>
    /// Controller for displaying errors.
    /// </summary>
    public class ErrorController : BaseController
    {
        public ErrorController(IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig) :
            base(dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Displays error message.
        /// </summary>
        /// <returns>Returns the dashboard page.</returns>
        public IActionResult Index()
        {
            return View("Error");
        }

        /// <summary>
        /// Log javascript errors to elmah.
        /// </summary>
        /// <param name="message"></param>
        public void LogJavascriptError(string message)
        {
            new JavaScriptException(message).Log();
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