using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dash
{
    public sealed class PjaxAttribute : ActionFilterAttribute
    {
        public PjaxAttribute(bool isPjax = true) => IsPjax = isPjax;

        public bool IsPjax { get; set; }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (!IsPjax)
                return;
            if (!((Controller)context.Controller).ViewBag.IsPjaxRequest)
                return;
            if (!context.HttpContext.Response.Headers.ContainsKey(PjaxConstants.PjaxVersion))
                context.HttpContext.Response.Headers.Add(PjaxConstants.PjaxVersion, PjaxConstants.PjaxVersionValue);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!IsPjax)
                return;
            var pjaxController = (Controller)context.Controller;
            var pjax = context.HttpContext.Request.Headers[PjaxConstants.PjaxHeader];
            pjaxController.ViewBag.IsPjaxRequest = bool.TryParse(pjax, out var isPjaxRequest) && isPjaxRequest;
            pjaxController.ViewBag.PjaxVersion = PjaxConstants.PjaxVersionValue;
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext.HttpContext.Response.StatusCode != StatusCodes.Status200OK && !filterContext.HttpContext.Response.Headers.ContainsKey(PjaxConstants.PjaxUrl))
                filterContext.HttpContext.Response.Headers.Add(PjaxConstants.PjaxUrl, filterContext.HttpContext.Request.Path.Value);
        }
    }
}
