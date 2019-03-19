using System.Linq;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dash
{
    public sealed class ValidModelAttribute : ActionFilterAttribute
    {
        readonly bool UseTempData;

        public ValidModelAttribute(bool useTempData = false) => UseTempData = useTempData;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var param = context.ActionArguments.FirstOrDefault(p => p.Value is BaseModel);
            if (param.Value == null)
                context.ModelState.AddModelError("general", Core.ErrorGeneric);

            if (!context.ModelState.IsValid)
            {
                var controller = (Controller)context.Controller;
                if (UseTempData)
                    controller.TempData["Error"] = context.ModelState.ToErrorString();
                else
                    controller.ViewBag.Error = context.ModelState.ToErrorString();
            }
        }
    }
}
