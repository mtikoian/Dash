using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
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

        public IActionResult JsonData(object data)
        {
            return Ok(data);
        }

        public IActionResult JsonComponent(Component component, string title, object data, bool isBasic = true)
        {
            return Ok(new {
                Component = component.ToString(),
                Title = title,
                Basic = isBasic,
                Data = data
            });
        }

        public IActionResult JsonError(string error)
        {
            return Ok(new { error });
        }

        public IActionResult JsonRows(IEnumerable<object> rows)
        {
            return Ok(new { Rows = rows });
        }

        public IActionResult JsonSuccess(string message = "")
        {
            if (message.IsEmpty())
            {
                return Ok(new { result = true });
            }
            return Ok(new { message });
        }

        protected RedirectToActionResult RedirectWithError(string controller, string action, string error, object routeValues = null)
        {
            TempData["Error"] = error;
            return RedirectToAction(action, routeValues);
        }
    }
}
