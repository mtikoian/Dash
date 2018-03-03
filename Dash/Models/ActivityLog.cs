using System;
using System.Linq;
using Dash.I18n;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    /// <summary>
    /// ActivityLog is only for logging user actions.
    /// </summary>
    public class ActivityLog : BaseModel
    {
        private IHttpContextAccessor _HttpContextAccessor;

        public ActivityLog(IHttpContextAccessor httpContextAccessor, DateTimeOffset requestTimestamp, int statusCode, string url, string method, string controller, string action, string requestData, long? duration)
        {
            _HttpContextAccessor = httpContextAccessor;
            RequestTimestamp = requestTimestamp;
            StatusCode = statusCode;
            Url = url;
            Method = method;
            Controller = controller;
            Action = action;
            RequestData = requestData;
            Duration = duration;
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public long? Duration { get; set; }

        /// <summary>
        /// Gets the current IP address.
        /// </summary>
        /// <returns>Host IP address.</returns>
        public string IP
        {
            get
            {
                var ip = "";
                if (_HttpContextAccessor?.HttpContext != null)
                {
                    ip = _HttpContextAccessor.HttpContext.Request.Headers.GetCommaSeparatedValues("HTTP_X_FORWARDED_FOR").Any()
                        ? _HttpContextAccessor.HttpContext.Request.Headers.GetCommaSeparatedValues("HTTP_X_FORWARDED_FOR").First()
                        : _HttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                // grab the first object in the string array if comma exists
                // comma means we have an x-forwarded-for header present
                return (ip.Contains(",") ? ip.Split(',').First().Trim() : ip).Trim();
            }
        }

        public string Method { get; set; }
        public string RequestData { get; set; }
        public DateTimeOffset RequestTimestamp { get; set; }
        public int StatusCode { get; set; }
        public string Url { get; set; }
    }
}