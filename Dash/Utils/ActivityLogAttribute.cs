using Jil;
using Dash.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dash
{
    /// <summary>
    /// Log requests to the db.
    /// </summary>
    public class ActivityLogAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Log the request.
        /// </summary>
        /// <param name="filterContext">Current filter context.</param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!filterContext.ActionDescriptor.GetCustomAttributes(typeof(SkipActivityLogAttribute), true).Any() && !filterContext.IsChildAction)
            {
                try
                {
                    new ActivityLog(DateTimeOffset.Now, filterContext.HttpContext.Response.StatusCode, filterContext.HttpContext.Request.Url.AbsolutePath,
                        filterContext.HttpContext.Request.HttpMethod, (filterContext.RouteData.Values["controller"] ?? "").ToString(),
                        (filterContext.RouteData.Values["action"] ?? "").ToString(), JSON.SerializeDynamic(RequestData(filterContext)), Stop(filterContext)?.ElapsedMilliseconds).Save();
                }
                catch
                {
                    // Catch db connection errors
                }
            }
            base.OnActionExecuted(filterContext);
        }

        /// <summary>
        /// Start the action timer.
        /// </summary>
        /// <param name="filterContext">Current filter context.</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(SkipActivityLogAttribute), false).Any())
            {
                Start(filterContext);
            }
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Combine request data (querystring/form/json) into a dictionary.
        /// </summary>
        /// <param name="context">Request controller context.</param>
        /// <returns>Dictionary of merge request values.</returns>
        private static Dictionary<string, object> RequestData(ControllerContext context)
        {
            var request = context.HttpContext.Request;
            var values = new Dictionary<string, object>();
            if (request.QueryString.Count > 0)
            {
                request.QueryString.AllKeys.Where(x => !x.IsEmpty()).ToList().ForEach(x => values[x] = request.QueryString[x]);
            }
            if (request.Form.Count > 0)
            {
                request.Form.AllKeys.Where(x => !x.IsEmpty()).ToList().ForEach(x => values[x] = request.Form[x]);
            }
            values.FilterRequestData();

            if (values.Count == 0 && request.ContentLength > 0)
            {
                string data = "";
                try
                {
                    request.InputStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(request.InputStream))
                    {
                        data = reader.ReadToEnd();
                    }
                    values.AddRange(JSON.Deserialize<Dictionary<string, object>>(data));
                }
                catch
                {
                    values["Data"] = data;
                }
            }
            values.FilterRequestData();

            return values;
        }

        /// <summary>
        /// Start a watch to time the request.
        /// </summary>
        /// <param name="context">Request controller context.</param>
        private void Start(ControllerContext context)
        {
            try
            {
                var watch = new Stopwatch();
                watch.Start();
                context.HttpContext.Items["Timer"] = watch;
            }
            catch { }
        }

        /// <summary>
        /// Stop the watch timing the request.
        /// </summary>
        /// <param name="context">Request controller context.</param>
        /// <returns>Stopwatch for the request.</returns>
        private Stopwatch Stop(ControllerContext context)
        {
            try
            {
                var watch = context.HttpContext.Items["Timer"] as Stopwatch;
                if (watch != null)
                {
                    watch.Stop();
                }
                return watch;
            }
            catch
            {
                return null;
            }
        }
    }

    public class SkipActivityLogAttribute : ActionFilterAttribute { }
}