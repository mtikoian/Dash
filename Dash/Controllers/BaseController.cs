using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Dash.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Controller constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="cache">App memory cache.</param>
        /// <param name="appConfig">App settings.</param>
        public BaseController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig)
        {
            HttpContextAccessor = httpContextAccessor;
            DbContext = dbContext;
            Cache = cache;
            AppConfig = appConfig;
        }

        public IAppConfiguration AppConfig { get; set; }
        public IMemoryCache Cache { get; set; }
        public IDbContext DbContext { get; set; }
        public IHttpContextAccessor HttpContextAccessor { get; set; }
        public int ID { get; set; }
        public BaseModel Model { get; set; }

        /// <summary>
        /// Gets an object from the cache. Cache persists for the lifetime of the app pool.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="key">Unique key to identify the object.</param>
        /// <param name="onCreate">If object doesn't exist in cache, use return value from this function to create the object.</param>
        /// <returns>Returns the matched object.</returns>
        public T Cached<T>(string key, Func<T> onCreate) where T : class
        {
            if (Cache == null)
            {
                return onCreate();
            }

            T result;
            if (!Cache.TryGetValue<T>(key, out result))
            {
                result = onCreate();
                Cache.Set(key, result);
            }
            return result;
        }

        /// <summary>
        /// Fetch model from db using ID.
        /// </summary>
        /// <returns>Returns true if model was found, else false.</returns>
        public bool HasValidDbModel<T>() where T : BaseModel
        {
            if (ID == 0)
            {
                return false;
            }
            Model = DbContext.Get<T>(ID);
            Model.RequestUserId = HttpContextAccessor.HttpContext.User?.Identity?.Name.ToInt();
            return Model != null;
        }

        /// <summary>
        /// Return a json error.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Returns object with error message.</returns>
        public JsonResult JsonError(string error)
        {
            return Json(new { error });
        }

        /// <summary>
        /// Return a list of json data for use with table.
        /// </summary>
        /// <param name="rows">List of data to return.</param>
        /// <returns>Returns object with row data.</returns>
        public JsonResult JsonRows(IEnumerable<object> rows)
        {
            return Json(new { Rows = rows });
        }

        /// <summary>
        /// Return a json object indicating success.
        /// </summary>
        /// <param name="message">Success message to return.</param>
        /// <returns>Returns object with message=message or result=true.</returns>
        public JsonResult JsonSuccess(string message = "")
        {
            if (message.IsEmpty())
            {
                return Json(new { result = true });
            }
            return Json(new { message });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <param name="error"></param>
        /// <param name="routeValues"></param>
        /// <returns></returns>
        protected RedirectToActionResult RedirectWithError(string controller, string action, string error, object routeValues = null)
        {
            TempData["Error"] = error;
            return RedirectToAction(action, routeValues);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <param name="message"></param>
        /// <param name="routeValues"></param>
        /// <returns></returns>
        protected RedirectToActionResult RedirectWithMessage(string controller, string action, string message, object routeValues = null)
        {
            TempData["Message"] = message;
            return RedirectToAction(action, routeValues);
        }
    }
}