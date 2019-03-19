using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    public class ErrorController : BaseController
    {
        public ErrorController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        public IActionResult Index(string code = null)
        {
            if (!code.IsEmpty())
            {
                var statusFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
                Serilog.Log.Error("Unhandled error code `{0}` for request `{1}`.", code, statusFeature?.OriginalPath);
            }
            return View("Error");
        }

        [HttpPost]
        public IActionResult LogJavascriptError([FromBody] JavascriptError error)
        {
            Serilog.Log.Error(new JavaScriptException(error.Message), "Javascript Exception");
            return Ok();
        }
    }
}
