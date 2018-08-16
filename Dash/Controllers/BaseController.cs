using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    public abstract class BaseController : Controller
    {
        public BaseController(IDbContext dbContext, AppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        protected IAppConfiguration AppConfig { get; set; }
        protected IDbContext DbContext { get; set; }
        protected int ID { get; set; }

        public IActionResult Data(object data)
        {
            return Ok(data);
        }

        public IActionResult Error(string error)
        {
            if (Request.IsAjaxRequest())
            {
                return Ok(new { error });
            }
            // this shouldn't happen, but its possible (IE if someone opens a reset password link while logged in), so show a safe error page as a fallback
            ViewBag.Error = error;
            return View("Error");
        }

        public IActionResult Rows(IEnumerable<object> rows)
        {
            return Ok(new { Rows = rows });
        }

        public IActionResult Success(string message = "")
        {
            if (!Request.IsAjaxRequest())
            {
                // this shouldn't happen
                ViewBag.Error = Core.ErrorGeneric;
                return View("Error");
            }
            if (message.IsEmpty())
            {
                return Ok(new { result = true });
            }
            return Ok(new { message });
        }
    }
}
