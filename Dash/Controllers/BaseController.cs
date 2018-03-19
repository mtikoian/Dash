using System;
using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
        public BaseController(IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig)
        {
            DbContext = dbContext;
            Cache = cache;
            AppConfig = appConfig;
        }

        protected IAppConfiguration AppConfig { get; set; }
        protected IMemoryCache Cache { get; set; }
        protected IDbContext DbContext { get; set; }
        protected int ID { get; set; }

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

            if (!Cache.TryGetValue<T>(key, out T result))
            {
                result = onCreate();
                Cache.Set(key, result);
            }
            return result;
        }

        /// <summary>
        /// Return a json error.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Returns object with error message.</returns>
        public IActionResult JsonError(string error)
        {
            return Ok(new { error });
        }

        /// <summary>
        /// Return a list of json data for use with table.
        /// </summary>
        /// <param name="rows">List of data to return.</param>
        /// <returns>Returns object with row data.</returns>
        public IActionResult JsonRows(IEnumerable<object> rows)
        {
            return Ok(new { Rows = rows });
        }

        /// <summary>
        /// Return a json object indicating success.
        /// </summary>
        /// <param name="message">Success message to return.</param>
        /// <returns>Returns object with message=message or result=true.</returns>
        public IActionResult JsonSuccess(string message = "")
        {
            if (message.IsEmpty())
            {
                return Ok(new { result = true });
            }
            return Ok(new { message });
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