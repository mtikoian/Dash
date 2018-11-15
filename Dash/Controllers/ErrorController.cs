using System;
using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    public class ErrorController : BaseController
    {
        public ErrorController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        public IActionResult Index(string code = null)
        {
            if (!code.IsEmpty())
            {
                var statusFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
                Serilog.Log.Error("Unhandled error code `{0}` for request `{1}`.", code, statusFeature?.OriginalPath);
            }
            return View("Error");
        }

        public void LogJavascriptError(string message)
        {
            Serilog.Log.Error(new JavaScriptException(message), "Javascript Exception");
        }
    }

    [Serializable]
    public class JavaScriptException : Exception
    {
        public JavaScriptException(string message) : base(message)
        {
        }
    }
}
