using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IAppConfiguration AppConfig { get; set; }

        protected IDbContext DbContext { get; set; }

        protected int ID { get; set; }

        public BaseController(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        public IActionResult Data(object data) => Ok(data);

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

        public IActionResult Rows(IEnumerable<object> rows) => Ok(new { Rows = rows });

        public IActionResult Success(string message = "")
        {
            if (!Request.IsAjaxRequest())
            {
                // this shouldn't happen
                ViewBag.Error = Core.ErrorGeneric;
                return View("Error");
            }
            return message.IsEmpty() ? Ok(new { result = true }) : Ok(new { message });
        }
    }
}
