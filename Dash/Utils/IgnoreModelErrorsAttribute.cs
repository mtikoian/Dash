using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dash
{
    /// <summary>
    /// Specify ModelState errors to ignore.
    /// </summary>
    public class IgnoreModelErrorsAttribute : ActionFilterAttribute
    {
        private string _Keys;

        public IgnoreModelErrorsAttribute(string keys)
            : base()
        {
            _Keys = keys;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var modelState = filterContext.ModelState;
            _Keys.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Each(x => {
                var keyPattern = x.Trim()
                    .Replace(@".", @"\.")
                    .Replace(@"[", @"\[")
                    .Replace(@"]", @"\]")
                    .Replace(@"\[\]", @"\[[0-9]+\]")
                    .Replace(@"*", @"[A-Za-z0-9]+");
                modelState.Keys.Where(y => Regex.IsMatch(y, keyPattern)).Each(y => modelState[y].Errors.Clear());
            });
        }
    }
}
