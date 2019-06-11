using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Dash.Models;
using Dash.Utils;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace Dash
{
    public static class Extensions
    {
        const string _RequestedWithHeader = "X-Requested-With";
        const string _XmlHttpRequest = "XMLHttpRequest";
        static Regex _CaseRegex = new Regex(@"([a-z])([A-Z])", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
        static Regex _CssRegex = new Regex(@"(?<!_)([A-Z])", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        /// <summary>
        /// Add an item to the dictionary if `add` is true.
        /// </summary>
        /// <param name="dict">Dictionary to update.</param>
        /// <param name="key">Key to add to dictionary if true.</param>
        /// <param name="value">Value to add to dictionary if true.</param>
        /// <param name="add">Add to dictionary if true.</param>
        /// <returns>Returns updated dictionary.</returns>
        public static TagHelperAttributeList AddIf(this TagHelperAttributeList dict, string key, string value, bool add)
        {
            if (add)
                dict.Add(key, value);
            return dict;
        }

        /// <summary>
        /// Add an item to the dictionary if `add` is true.
        /// </summary>
        /// <param name="dict">Dictionary to update.</param>
        /// <param name="key">Key to add to dictionary if true.</param>
        /// <param name="value">Value to add to dictionary if true.</param>
        /// <param name="add">Add to dictionary if true.</param>
        /// <returns>Returns updated dictionary.</returns>
        public static AttributeDictionary AddIf(this AttributeDictionary dict, string key, string value, bool add)
        {
            if (add)
                dict.Add(key, value);
            return dict;
        }

        /// <summary>
        /// Create/fetch objects from memory cache.
        /// </summary>
        /// <typeparam name="T">Type of object to pull from cache.</typeparam>
        /// <param name="cache">Memory cache instance.</param>
        /// <param name="key">Unique key of the item.</param>
        /// <param name="onCreate">Method to create item if it doesn't exist in cache.</param>
        /// <returns>Item from cache or result of onCreate function.</returns>
        public static T Cached<T>(this IMemoryCache cache, string key, Func<T> onCreate) where T : class
        {
            if (cache == null)
                return onCreate();

            if (!cache.TryGetValue<T>(key, out var result))
            {
                result = onCreate();
                cache.Set(key, result);
            }
            return result;
        }

        /// <summary>
        /// Create a new copy of an object.
        /// </summary>
        /// <typeparam name="T">Type of the object to create.</typeparam>
        /// <param name="obj">Object to copy.</param>
        /// <returns>New instance of type T with values from obj.</returns>
        public static T Clone<T>(this T obj)
        {
            var accessor = TypeAccessor.Create(typeof(T));
            var newObj = accessor.CreateNew();
            var properties = obj.GetType().GetProperties();
            foreach (var prop in properties)
                if (prop.CanWrite)
                    accessor[newObj, prop.Name] = accessor[obj, prop.Name];
            return (T)newObj;
        }

        /// <summary>
        /// Helper for iterating over ienumerables.
        /// </summary>
        /// <typeparam name="T">Type of item in the list.</typeparam>
        /// <param name="list">List to iterate over.</param>
        /// <param name="action">Action to perform</param>
        /// <returns></returns>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> list, Action<T> action)
        {
            if (list == null)
                return list;
            foreach (var x in list)
                action(x);
            return list;
        }

        /// <summary>
        /// Get the end time of the hour for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfHour(this DateTime dt) => dt.StartOfHour().AddMinutes(60);

        /// <summary>
        /// Get the end time of the minute for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfMinute(this DateTime dt) => dt.StartOfMinute().AddSeconds(60);

        /// <summary>
        /// Get the end time of the month for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfMonth(this DateTime dt) => dt.StartOfMonth().AddMonths(1).AddMilliseconds(-1);

        /// <summary>
        /// Get the end time of the quarter for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfQuarter(this DateTime dt)
        {
            var currQuarter = (dt.Month - 1) / 3 + 1;
            var year = dt.Year;
            var quarter = 3 * currQuarter + 1;
            if (quarter > 12)
            {
                year += 1;
                quarter -= 12;
            }
            return new DateTime(year, quarter, 1).AddMilliseconds(-1);
        }

        /// <summary>
        /// Get the end time of the week for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfWeek(this DateTime dt) => dt.StartOfWeek().AddDays(7).AddMilliseconds(-1);

        /// <summary>
        /// Get the end time of the year for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfYear(this DateTime dt) => dt.StartOfYear().AddYears(1).AddMilliseconds(-1);

        /// <summary>
        /// Get an attribute for a property if it exists.
        /// </summary>
        /// <typeparam name="T">Attribute to check for.</typeparam>
        /// <param name="member">Property to check against.</param>
        /// <returns>Returns attribute if member has the attribute, else false.</returns>
        public static T GetMemberAttribute<T>(this Member member) where T : Attribute => GetPrivateField<MemberInfo>(member, "member").GetCustomAttribute<T>();

        /// <summary>
        /// Get details about a private field in a class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetPrivateField<T>(this object obj, string name)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            var field = type.GetField(name, flags);
            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// Check if the user has access to a controller/action combo.
        /// </summary>
        /// <param name="claimsPrincipal">Claims principal for user.</param>
        /// <param name="controller">Requested controller.</param>
        /// <param name="action">Requested action.</param>
        /// <returns>True if user has access, else false.</returns>
        public static bool HasAccess(this ClaimsPrincipal claimsPrincipal, string controller, string action, HttpVerbs method = HttpVerbs.Get) => new ControllerAction(controller, action, method).EffectivePermissions().Any(x => claimsPrincipal.IsInRole(x));

        /// <summary>
        /// Check if a member has an attribute assigned to it.
        /// </summary>
        /// <typeparam name="T">Attribute to check for.</typeparam>
        /// <param name="member">Property to check against.</param>
        /// <returns>Returns true if member has attribute set, else false.</returns>
        public static bool HasAttribute<T>(this Member member) where T : Attribute => member.GetMemberAttribute<T>() != null;

        /// <summary>
        /// Check if an integer has a value greater than zero.
        /// </summary>
        /// <param name="value">Integer to check.</param>
        /// <returns>Returns true if the integer is not null and greater than zero.</returns>
        public static bool HasPositiveValue(this int? value) => value.HasValue && value.Value > 0;

        /// <summary>
        /// Check if the request object is an AJAX request.
        /// </summary>
        /// <param name="request">Current request object.</param>
        /// <returns>True if is an ajax request, else false.</returns>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Headers != null)
                return request.Headers[_RequestedWithHeader] == _XmlHttpRequest;
            return false;
        }

        /// <summary>
        /// Check if list or viewList contains an ID.
        /// </summary>
        /// <typeparam name="T">List type</typeparam>
        /// <param name="list">First list to check in.</param>
        /// <param name="expression">Expression to get ID to look for.</param>
        /// <param name="viewList">List of integers to check for ID in.</param>
        /// <param name="value">ID to check for.</param>
        /// <returns></returns>
        public static bool IsChecked<T>(IEnumerable<T> list, Func<T, bool> expression, int[] viewList, int value) => (list != null && list.Any(expression)) || (viewList != null && viewList.Contains(value));

        /// <summary>
        /// Check if a string is empty or null.
        /// </summary>
        /// <param name="value">String to check.</param>
        /// <returns>True if string is not null or empty.</returns>
        public static bool IsEmpty(this string value) => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Join a list of string using separator.
        /// </summary>
        /// <param name="value">String list to combine.</param>
        /// <param name="separator">String to use between list items.</param>
        /// <returns>Joined string.</returns>
        public static string Join(this IEnumerable<string> value, string separator = ", ") => string.Join(separator, value);

        /// <summary>
        /// Prepend an item to the beginning of a list.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="list">List to prepend to.</param>
        /// <param name="item">Item to prepend.</param>
        /// <param name="when">Only prepend when when istrue.</param>
        /// <returns></returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> list, T item, bool when = true)
        {
            var res = list.ToList();
            if (when)
                res.Insert(0, item);
            return res;
        }

        /// <summary>
        /// Returns a trimmed string. If longer than max length includes ellipsis at end.
        /// </summary>
        /// <param name="value">Value to break up.</param>
        /// <param name="maxLength">Maximum length of returned string.</param>
        /// <returns>Returns a pretty string.</returns>
        public static string PrettyTrim(this string value, int maxLength)
        {
            if (value.IsEmpty())
                return value;
            if (value.Length > (maxLength - 4))
                return $"{value.Substring(0, maxLength - 4)} ...";
            return value;
        }

        /// <summary>
        /// Get the quarter of the year for a dae.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>Quarter number.</returns>
        public static int Quarter(this DateTime dt) => (dt.Month + 2) / 3;

        /// <summary>
        /// String replace that allows you to specify case handling.
        /// Via http://stackoverflow.com/a/244933
        /// </summary>
        /// <param name="str">String to update.</param>
        /// <param name="oldValue">Value to search for.</param>
        /// <param name="newValue">Value to replace oldValue with.</param>
        /// <param name="comparison">Comparison type.</param>
        /// <returns>Returns the updated string.</returns>
        public static string ReplaceCase(this string str, string oldValue, string newValue, StringComparison comparison = StringComparison.CurrentCultureIgnoreCase)
        {
            var sb = new StringBuilder();
            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));
            return sb.ToString();
        }

        /// <summary>
        /// Get the start time of the hour for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfHour(this DateTime dt) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);

        /// <summary>
        /// Get the start time of the minute for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfMinute(this DateTime dt) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);

        /// <summary>
        /// Get the start time of the month for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfMonth(this DateTime dt) => new DateTime(dt.Year, dt.Month, 1);

        /// <summary>
        /// Get the start time of the quarter for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfQuarter(this DateTime dt) => new DateTime(dt.Year, 3 * ((dt.Month - 1) / 3 + 1) - 2, 1);

        /// <summary>
        /// Get the start time of the week for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfWeek(this DateTime dt)
        {
            var culture = CultureInfo.CurrentCulture;
            var diff = dt.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;
            if (diff < 0)
                diff += 7;
            return dt.AddDays(-diff).Date;
        }

        /// <summary>
        /// Get the start time of the year for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfYear(this DateTime dt) => new DateTime(dt.Year, 1, 1);

        /// <summary>
        /// Converts a string value to a boolean. Default to false.
        /// </summary>
        /// <param name="val">Value to attempt to convert.</param>
        /// <returns>Bool value</returns>
        public static bool ToBool(this string val) => val != null && (val == "1" || val.ToLower() == "true");

        /// <summary>
        /// Sanitize a filename for filesystem and add extension.
        /// </summary>
        /// <param name="fileName">Name to sanitize.</param>
        /// <param name="extension">Extension to add to file name.</param>
        /// <returns>Sanitized file name with extension.</returns>
        public static string ToCleanFileName(this string fileName, string extension)
        {
            var formattedName = fileName;
            Array.ForEach(Path.GetInvalidFileNameChars(), c => formattedName = formattedName.Replace(c.ToString(), string.Empty));
            return $"{formattedName}.{extension}";
        }

        /// <summary>
        /// Convert a DataRow with table schema data into a delimited column name.
        /// </summary>
        /// <param name="row">DataRow with schema info.</param>
        /// <param name="hasSchema">False if MySQL server, else true.</param>
        /// <returns>Returns the fully qualified, delimited column name.</returns>
        public static string ToColumnName(this DataRow row, bool hasSchema = true) => hasSchema ? $"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}.{row["COLUMN_NAME"]}" : $"{row["TABLE_NAME"]}.{row["COLUMN_NAME"]}";

        /// <summary>
        /// Convert a button enum to a css class name.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Css class name string.</returns>
        public static string ToCssClass(this DashButtons val) => _CssRegex.Replace(val.ToString(), "-$1").Trim('-').ToLower();

        /// <summary>
        /// Convert an icon enum to a css class name.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Css class name string.</returns>
        public static string ToCssClass(this DashIcons val) => _CssRegex.Replace(val.ToString(), "-$1").Trim('-').ToLower();

        /// <summary>
        /// Convert an object with a datetime into a datetime object.
        /// </summary>
        /// <param name="val">String value to attempt to convert.</param>
        /// <returns>DateTime value.</returns>
        public static DateTime ToDateTime(this object val)
        {
            var res = new DateTime();
            DateTime.TryParse(val.ToString(), out res);
            return res;
        }

        /// <summary>
        /// Convert a string to a double. Defaults to zero.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Integer value.</returns>
        public static double ToDouble(this string val)
        {
            double.TryParse(val, out var res);
            return res;
        }

        /// <summary>
        /// Convert the ModelStateDictionary into a string of errors a view can display.
        /// </summary>
        /// <param name="state">State of a model.</param>
        /// <returns>Space separated list of errors.</returns>
        public static string ToErrorString(this ModelStateDictionary state) => string.Join(" <br />", state.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray());

        /// <summary>
        /// Convert list of validation errors into a string of errors a view can display.
        /// </summary>
        /// <param name="errors">List of validation results.</param>
        /// <returns>Space separated list of errors.</returns>
        public static string ToErrorString(this IEnumerable<ValidationResult> errors) => string.Join(" <br />", errors.Select(x => x.ErrorMessage).ToArray());

        /// <summary>
        /// Convert an integer into an Excel column name.
        /// </summary>
        /// <param name="column">Column number</param>
        /// <returns>Excel friendly column name.</returns>
        public static string ToExcelColumn(this int column)
        {
            var columnString = "";
            var columnNumber = (decimal)column;
            while (columnNumber > 0)
            {
                var currentLetterNumber = (columnNumber - 1) % 26;
                columnString = (char)(currentLetterNumber + 65) + columnString;
                columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
            }
            return columnString;
        }

        /// <summary>
        /// Convert a pascal case string to hyphen case. IE "QuickBrownFoxJumpsOverTheLazyDog" to "quick-brown-fox-jumps-over-the-lazy-dog"
        /// </summary>
        /// <param name="value">Toggle enum value to convert.</param>
        /// <returns>Converted string.</returns>
        public static string ToHyphenCase(this DataToggles toggle) => _CaseRegex.Replace(toggle.ToString(), "$1-$2").ToLower();

        /// <summary>
        /// Convert a pascal case string to hyphen case. IE "QuickBrownFoxJumpsOverTheLazyDog" to "quick-brown-fox-jumps-over-the-lazy-dog"
        /// </summary>
        /// <param name="value">Toggle enum value to convert.</param>
        /// <returns>Converted string.</returns>
        public static string ToHyphenCase(this DataToggles? toggle) => toggle.HasValue ? toggle.Value.ToHyphenCase() : "";

        /// <summary>
        /// Convert a string to an int. Defaults to zero.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Integer value.</returns>
        public static int ToInt(this string val)
        {
            if (val == null)
                return 0;
            int.TryParse(val, out var res);
            return res;
        }

        /// <summary>
        /// Convert a object to an int. Defaults to zero.
        /// </summary>
        /// <param name="val">Object value to convert.</param>
        /// <returns>Integer value.</returns>
        public static int ToInt(this object val) => (val.ToString() ?? "").ToInt();

        /// <summary>
        /// Adjust a datetime to the closest matching interval step (ie 11:07 for hour interval becomes 11:00).
        /// </summary>
        /// <param name="dt">Datetime to convert.</param>
        /// <param name="dateInterval">Interval to convert to.</param>
        /// <returns>Returns the adjusted datetime.</returns>
        public static string ToInterval(this DateTime dt, DateIntervals dateInterval)
        {
            switch (dateInterval)
            {
                case DateIntervals.Day:
                    return string.Format("{0:yyyy-MM-dd}", dt);
                case DateIntervals.FifteenMinutes:
                    return string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 15) * 15);
                case DateIntervals.FiveMinutes:
                    return string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 5) * 5);
                case DateIntervals.Hour:
                    return string.Format("{0:yyyy-MM-dd HH:00}", dt);
                case DateIntervals.Month:
                    return string.Format("{0:yyyy-MM}", dt);
                case DateIntervals.OneMinute:
                    return string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, dt.Minute);
                case DateIntervals.Quarter:
                    return string.Format("{0} Q{1}", dt.Year, dt.Quarter());
                case DateIntervals.TenMinutes:
                    return string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 10) * 10);
                case DateIntervals.ThirtyMinutes:
                    return string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 30) * 30);
                case DateIntervals.Week:
                    return string.Format("{0} W{1}", dt.Year, dt.Week());
                default:
                    return string.Format("{0:yyyy}", dt);
            }
        }

        /// <summary>
        /// Find the correct step size for a time interval.
        /// </summary>
        /// <param name="dateInterval">Interval to convert to.</param>
        /// <returns>Returns the correct sized timespan.</returns>
        public static TimeSpan ToIntervalStep(this DateIntervals dateInterval)
        {
            switch (dateInterval)
            {
                case DateIntervals.Day:
                    return new TimeSpan(24, 0, 0);
                case DateIntervals.FifteenMinutes:
                    return new TimeSpan(0, 15, 0);
                case DateIntervals.FiveMinutes:
                    return new TimeSpan(0, 5, 0);
                case DateIntervals.Hour:
                    return new TimeSpan(1, 0, 0);
                case DateIntervals.Month:
                    // rough approximation
                    return new TimeSpan(Math.Round(24 * 30.436875).ToInt(), 0, 0);
                case DateIntervals.OneMinute:
                    return new TimeSpan(0, 1, 0);
                case DateIntervals.Quarter:
                    // rough approximation
                    return new TimeSpan(Math.Round(24 * 91.25).ToInt(), 0, 0);
                case DateIntervals.TenMinutes:
                    return new TimeSpan(0, 10, 0);
                case DateIntervals.ThirtyMinutes:
                    return new TimeSpan(0, 30, 0);
                case DateIntervals.Week:
                    return new TimeSpan(24 * 7, 0, 0);
                default:
                    // rough approximation
                    return new TimeSpan(24 * 365, 0, 0);
            }
        }

        /// <summary>
        /// Convert a string to an long. Defaults to zero.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Long value.</returns>
        public static long ToLong(this string val)
        {
            if (val == null)
                return 0;
            long.TryParse(val, out var res);
            return res;
        }

        /// <summary>
        /// Convert a object to an long. Defaults to zero.
        /// </summary>
        /// <param name="val">Object value to convert.</param>
        /// <returns>Long value.</returns>
        public static long ToLong(this object val) => (val.ToString() ?? "").ToLong();

        /// <summary>
        /// Convert IEnumerable to a list of select list items.
        /// </summary>
        /// <typeparam name="T">Enumerable type.</typeparam>
        /// <param name="enumerable">List of items to convert.</param>
        /// <param name="text">Function to get the option text.</param>
        /// <param name="value">Funciton to get the option value.</param>
        /// <returns>List of select list items.</returns>
        public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> enumerable, Func<T, string> text, Func<T, string> value) => enumerable.Select(f => new SelectListItem { Text = text(f), Value = value(f) }).ToList();

        /// <summary>
        /// Format a datetime into a string SQL will always understand - to avoid culture issues.
        /// </summary>
        /// <param name="val">Date value to format.</param>
        /// <returns>DateTime value.</returns>
        public static string ToSqlDateTime(this DateTime val) => val.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// Format a timespan into a string SQL will always understand - to avoid culture issues.
        /// </summary>
        /// <param name="val">Timespan value to format.</param>
        /// <returns>Time value.</returns>
        public static string ToSqlTime(this TimeSpan val) => val.ToString("HH:mm:ss");

        /// <summary>
        /// Convert a DataRow with table schema data into a fully qualified, delimited table name.
        /// </summary>
        /// <param name="row">DataRow with schema info.</param>
        /// <param name="hasSchema">False if MySQL server, else true.</param>
        /// <returns>Returns the delimited table name.</returns>
        public static string ToTableName(this DataRow row, bool hasSchema = true) => hasSchema ? $"{row["TABLE_SCHEMA"]}.{row["TABLE_NAME"]}" : $"{row["TABLE_NAME"]}";

        /// <summary>
        /// Convert an object with a time into a timespan object.
        /// </summary>
        /// <param name="val">String value to attempt to convert.</param>
        /// <returns>Timespan value.</returns>
        public static TimeSpan ToTimespan(this object val)
        {
            var res = new TimeSpan();
            TimeSpan.TryParse(val.ToString(), out res);
            return res;
        }

        /// <summary>
        /// Converts Enumeration type into an object of ids and names.
        /// </summary>
        /// <param name="t">Enum type</param>
        public static IEnumerable<SelectListItem> TranslatedSelect(this Type t, ResourceDictionary resource = null, string prefix = "")
        {
            if (t == null || !t.IsEnum)
                return null;

            var intValues = Enum.GetValues(t).Cast<int>().ToArray();
            if (resource != null)
                return (from i in intValues select new SelectListItem() { Text = resource[prefix + Enum.GetName(t, i)], Value = i.ToString() }).OrderBy(x => x.Text).ThenBy(x => x.Value);
            return (from i in intValues select new SelectListItem() { Text = Enum.GetName(t, i), Value = i.ToString() }).OrderBy(x => x.Text).ThenBy(x => x.Value);
        }

        /// <summary>
        /// Uppercase the first character of a string.
        /// </summary>
        /// <param name="value">String to update.</param>
        /// <returns>Updated string.</returns>
        public static string UppercaseFirst(this string value) => value.IsEmpty() ? string.Empty : (char.ToUpper(value[0]) + value.Substring(1));

        /// <summary>
        /// Get the user ID from the claims.
        /// </summary>
        /// <param name="claimsPrincipal">Claims principal for user.</param>
        /// <returns>UserID if available, else null.</returns>
        public static int UserId(this ClaimsPrincipal claimsPrincipal) => claimsPrincipal?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value.ToInt() ?? 0;

        /// <summary>
        /// Check if user has help enabled.
        /// </summary>
        /// <param name="httpContext">Current request context.</param>
        /// <returns>True if user enabled help, else false.</returns>
        public static bool WantsHelp(this HttpContext httpContext) => httpContext.Session.GetString(Help.SettingName).ToBool();

        /// <summary>
        /// Check if user has profiler enabled.
        /// </summary>
        /// <param name="httpContext">Current request context.</param>
        /// <returns>True if user enabled help, else false.</returns>
        public static bool WantsProfiling(this HttpContext httpContext) => httpContext.Session.GetString(Profiling.SettingName).ToBool();

        /// <summary>
        /// Get the week of the year for a date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>Week number</returns>
        public static int Week(this DateTime dt) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
