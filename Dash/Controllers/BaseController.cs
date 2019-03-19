using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    public abstract class BaseController : Controller
    {
        User _CurrentUser;

        protected IAppConfiguration AppConfig { get; set; }
        protected User CurrentUser => _CurrentUser ?? (_CurrentUser = DbContext?.Get<User>(User.UserId()));
        protected IDbContext DbContext { get; set; }
        protected int ID { get; set; }

        protected bool LoadModel<T>(int id, out T model, bool useTempData = false) where T : BaseModel
        {
            if ((model = DbContext.Get<T>(id)) != null)
                return true;

            if (useTempData)
                TempData["Error"] = Core.ErrorInvalidId;
            else
                ViewBag.Error = Core.ErrorInvalidId;
            return false;
        }

        public BaseController(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        public IActionResult Data(object data) => Ok(data);

        public IActionResult Error(string error)
        {
            if (Request.IsAjaxRequest())
                return Ok(new { error });

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
