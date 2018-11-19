﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Dash.Models;
using Dash.Utils;
using FastMember;
using Jil;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash
{
    public static class Extensions
    {
        private const string RequestedWithHeader = "X-Requested-With";
        private const string XmlHttpRequest = "XMLHttpRequest";
        private static Regex CssRegex = new Regex(@"(?<!_)([A-Z])", RegexOptions.Compiled);
        private static Regex CsvRegex = new Regex(@"(?:^|(,\s?))(""(?:[^""]+|"""")*""|[^(,\s?)]*)", RegexOptions.Compiled);

        /// <summary>
        /// Add an item to the list if `add` is true.
        /// </summary>
        /// <typeparam name="T">Type of item in the list.</typeparam>
        /// <param name="list">List to update.</param>
        /// <param name="add">Add to list if true.</param>
        /// <returns>Returns updated list.</returns>
        public static IEnumerable<T> AddIf<T>(this IEnumerable<T> list, T item, bool add)
        {
            return add ? list.Append(item) : list;
        }

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
            {
                dict.Add(key, value);
            }
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
            {
                dict.Add(key, value);
            }
            return dict;
        }

        /// <summary>
        /// Add a line break to a string if not empty.
        /// </summary>
        /// <param name="value">String to update</param>
        /// <returns>Updated string.</returns>
        public static string AddLine(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value + "\n";
        }

        /// <summary>
        /// Add a range to an existing dictionary.
        /// </summary>
        /// <typeparam name="T">Type of key</typeparam>
        /// <param name="target">Dictionary to add elements to</param>
        /// <param name="source">Dictionary of elements to add</param>
        public static void AddRange<T, T2>(this IDictionary<T, T2> target, IDictionary<T, T2> source)
        {
            source.Each(x => {
                if (target.ContainsKey(x.Key))
                {
                    target[x.Key] = x.Value;
                }
                else
                {
                    target.Add(x);
                }
            });
        }

        /// <summary>
        /// Add/update a value in a dictionary.
        /// </summary>
        /// <param name="dict">Dictionary to update</param>
        /// <param name="key">Key for the new value</param>
        /// <param name="value">Value to append.</param>
        public static Dictionary<string, string> Append(this Dictionary<string, string> dictionary, string key, string value, string separator = " ")
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] += separator + value;
            }
            else
            {
                dictionary[key] = value;
            }
            return dictionary;
        }

        /// <summary>
        /// Add/replace a value in a dictionary.
        /// </summary>
        /// <param name="dict">Dictionary to update</param>
        /// <param name="key">Key for the new value</param>
        /// <param name="value">Value to append.</param>
        public static Dictionary<string, object> Append(this Dictionary<string, object> dictionary, string key, object value)
        {
            dictionary[key] = dictionary.ContainsKey(key) ? dictionary[key] + " " + value : value;
            return dictionary;
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
            {
                if (prop.CanWrite)
                {
                    accessor[newObj, prop.Name] = accessor[obj, prop.Name];
                }
            }
            return (T)newObj;
        }

        /// <summary>
        /// Breaks a JSON array, or comma delimited string into a list.
        /// </summary>
        /// <param name="value">Value to break up.</param>
        /// <returns>Returns a list of values.</returns>
        public static List<object> Delimit(this string value)
        {
            var result = new List<object>();
            if (value.Substring(0, 1) == "[")
            {
                var jsonArr = JSON.Deserialize<List<string>>(value);
                if (jsonArr != null && jsonArr.Any())
                {
                    result = jsonArr.Select(x => (object)x.Trim()).ToList();
                    return result;
                }
            }

            foreach (Match match in CsvRegex.Matches(value))
            {
                result.Add(match.Value.TrimStart(',').Trim().TrimStart('"').TrimEnd('"'));
            }
            return result;
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
            {
                return list;
            }
            foreach (var x in list)
            {
                action(x);
            }
            return list;
        }

        /// <summary>
        /// Get the end time of the month for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfMonth(this DateTime dt)
        {
            return dt.StartOfMonth().AddMonths(1).AddMilliseconds(-1);
        }

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
        public static DateTime EndOfWeek(this DateTime dt)
        {
            return dt.StartOfWeek().AddDays(7).AddMilliseconds(-1);
        }

        /// <summary>
        /// Get the end time of the year for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime EndOfYear(this DateTime dt)
        {
            return dt.StartOfYear().AddYears(1).AddMilliseconds(-1);
        }

        /// <summary>
        /// Get an attribute for a property if it exists.
        /// </summary>
        /// <typeparam name="T">Attribute to check for.</typeparam>
        /// <param name="member">Property to check against.</param>
        /// <returns>Returns attribute if member has the attribute, else false.</returns>
        public static T GetMemberAttribute<T>(this Member member) where T : Attribute
        {
            return GetPrivateField<MemberInfo>(member, "member").GetCustomAttribute<T>();
        }

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
        public static bool HasAccess(this ClaimsPrincipal claimsPrincipal, string controller, string action, HttpVerbs method = HttpVerbs.Get)
        {
            var permissions = new ControllerAction(controller, action, method).EffectivePermissions();
            return permissions.Any(x => claimsPrincipal.IsInRole(x));
        }

        /// <summary>
        /// Check if a member has an attribute assigned to it.
        /// </summary>
        /// <typeparam name="T">Attribute to check for.</typeparam>
        /// <param name="member">Property to check against.</param>
        /// <returns>Returns true if member has attribute set, else false.</returns>
        public static bool HasAttribute<T>(this Member member) where T : Attribute
        {
            return member.GetMemberAttribute<T>() != null;
        }

        /// <summary>
        /// Check if an integer has a value greater than zero.
        /// </summary>
        /// <param name="value">Integer to check.</param>
        /// <returns>Returns true if the integer is not null and greater than zero.</returns>
        public static bool HasPositiveValue(this int? value)
        {
            return value.HasValue && value.Value > 0;
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (request.Headers != null)
            {
                return request.Headers[RequestedWithHeader] == XmlHttpRequest;
            }
            return false;
        }

        public static bool IsChecked<T>(IEnumerable<T> list, Func<T, bool> expression, int[] viewList, int value)
        {
            return (list != null && list.Any(expression)) || (viewList != null && viewList.Contains(value));
        }

        /// <summary>
        /// Check if a string is empty or null.
        /// </summary>
        /// <param name="value">String to check.</param>
        /// <returns>True if string is not null or empty.</returns>
        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Join a list of string using separator.
        /// </summary>
        /// <param name="value">String list to combine.</param>
        /// <param name="separator">String to use between list items.</param>
        /// <returns>Joined string.</returns>
        public static string Join(this IEnumerable<string> value, string separator = ", ")
        {
            return string.Join(separator, value);
        }

        /// <summary>
        /// Add a value to a dictionary, overwriting if it exists. This method lets me add to dictionary in a chainable way.
        /// </summary>
        /// <typeparam name="TKey">Type of the key for the item.</typeparam>
        /// <typeparam name="TValue">Type of the new value.</typeparam>
        /// <param name="dictionary">Dictionary to update.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">New value.</param>
        /// <returns>Returns an updated dictionary.</returns>
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary[key] = value;
            return dictionary;
        }

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
            {
                res.Insert(0, item);
            }
            return res;
        }

        /// <summary>
        /// Get the quarter of the year for a dae.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>Quarter number.</returns>
        public static int Quarter(this DateTime dt)
        {
            return (dt.Month + 2) / 3;
        }

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
        /// Get the start time of the month for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// Get the start time of the quarter for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfQuarter(this DateTime dt)
        {
            var currQuarter = (dt.Month - 1) / 3 + 1;
            return new DateTime(dt.Year, 3 * currQuarter - 2, 1);
        }

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
            {
                diff += 7;
            }
            return dt.AddDays(-diff).Date;
        }

        /// <summary>
        /// Get the start time of the year for the date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>New dateTime</returns>
        public static DateTime StartOfYear(this DateTime dt)
        {
            return new DateTime(dt.Year, 1, 1);
        }

        /// <summary>
        /// Converts a string value to a boolean. Default to false.
        /// </summary>
        /// <param name="val">Value to attempt to convert.</param>
        /// <returns>Bool value</returns>
        public static bool ToBool(this string val)
        {
            return val != null && (val == "1" || val.ToLower() == "true");
        }

        /// <summary>
        /// Convert a DataRow with table schema data into a delimited column name.
        /// </summary>
        /// <param name="row">DataRow with schema info.</param>
        /// <param name="isSqlServer">True if SQL server, else MySQL.</param>
        /// <param name="fullyQualified">If true use fully qualified schema/table/column name, if false just column name.</param>
        /// <returns>Returns the delimited column name.</returns>
        public static string ToColumnName(this DataRow row, bool isSqlServer = true, bool fullyQualified = true)
        {
            if (fullyQualified)
            {
                return isSqlServer ? $"[{row["TABLE_SCHEMA"]}].[{row["TABLE_NAME"]}].[{row["COLUMN_NAME"]}]" : $"`{row["TABLE_NAME"]}`.`{row["COLUMN_NAME"]}`";
            }
            return isSqlServer ? $"[{row["COLUMN_NAME"]}]" : $"`{row["COLUMN_NAME"]}`";
        }

        /// <summary>
        /// Convert a button enum to a css class name.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Css class name string.</returns>
        public static string ToCssClass(this DashButtons val)
        {
            return CssRegex.Replace(val.ToString(), "-$1").Trim('-').ToLower();
        }

        /// <summary>
        /// Convert an icon enum to a css class name.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Css class name string.</returns>
        public static string ToCssClass(this DashIcons val)
        {
            return CssRegex.Replace(val.ToString(), "-$1").Trim('-').ToLower();
        }

        /// <summary>
        /// Convert an enum to a css class name.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Css class name string.</returns>
        public static string ToCssClassList(this IEnumerable<DashButtons> classes)
        {
            return string.Join(" ", classes.Select(x => x.ToCssClass()));
        }

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
        public static string ToErrorString(this ModelStateDictionary state)
        {
            return string.Join(" <br />", state.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray());
        }

        /// <summary>
        /// Convert list of validation errors into a string of errors a view can display.
        /// </summary>
        /// <param name="errors">List of validation results.</param>
        /// <returns>Space separated list of errors.</returns>
        public static string ToErrorString(this IEnumerable<ValidationResult> errors)
        {
            return string.Join(" <br />", errors.Select(x => x.ErrorMessage).ToArray());
        }

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
        /// Convert a string to an int. Defaults to zero.
        /// </summary>
        /// <param name="val">String value to convert.</param>
        /// <returns>Integer value.</returns>
        public static int ToInt(this string val)
        {
            if (val == null)
            {
                return 0;
            }
            int.TryParse(val, out var res);
            return res;
        }

        /// <summary>
        /// Convert a object to an int. Defaults to zero.
        /// </summary>
        /// <param name="val">Object value to convert.</param>
        /// <returns>Integer value.</returns>
        public static int ToInt(this object val)
        {
            return (val.ToString() ?? "").ToInt();
        }

        /// <summary>
        /// Adjust a datetime to the closest matching interval step (ie 11:07 for hour interval becomes 11:00).
        /// </summary>
        /// <param name="dt">Datetime to convert.</param>
        /// <param name="dateInterval">Interval to convert to.</param>
        /// <returns>Returns the adjusted datetime.</returns>
        public static string ToInterval(this DateTime dt, DateIntervals dateInterval)
        {
            var interval = "";
            switch (dateInterval)
            {
                case DateIntervals.Day:
                    interval = string.Format("{0:yyyy-MM-dd}", dt);
                    break;
                case DateIntervals.FifteenMinutes:
                    interval = string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 15) * 15);
                    break;
                case DateIntervals.FiveMinutes:
                    interval = string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 5) * 5);
                    break;
                case DateIntervals.Hour:
                    interval = string.Format("{0:yyyy-MM-dd HH:00}", dt);
                    break;
                case DateIntervals.Month:
                    interval = string.Format("{0:yyyy-MM}", dt);
                    break;
                case DateIntervals.OneMinute:
                    interval = string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, dt.Minute);
                    break;
                case DateIntervals.Quarter:
                    interval = string.Format("{0} Q{1}", dt.Year, dt.Quarter());
                    break;
                case DateIntervals.TenMinutes:
                    interval = string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 10) * 10);
                    break;
                case DateIntervals.ThirtyMinutes:
                    interval = string.Format("{0:yyyy-MM-dd HH}:{1:00}", dt, (dt.Minute / 30) * 30);
                    break;
                case DateIntervals.Week:
                    interval = string.Format("{0} W{1}", dt.Year, dt.Week());
                    break;
                case DateIntervals.Year:
                    interval = string.Format("{0:yyyy}", dt);
                    break;
            }
            return interval;
        }

        public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> enumerable, Func<T, string> text, Func<T, string> value)
        {
            return enumerable.Select(f => new SelectListItem { Text = text(f), Value = value(f) }).ToList();
        }

        /// <summary>
        /// Format a datetime into a string SQL will always understand - to avoid culture issues.
        /// </summary>
        /// <param name="val">Date value to format.</param>
        /// <returns>DateTime value.</returns>
        public static string ToSqlDateTime(this DateTime val)
        {
            return val.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Convert a DataRow with table schema data into a fully qualified, delimited table name.
        /// </summary>
        /// <param name="row">DataRow with schema info.</param>
        /// <param name="isSqlServer">True if SQL server, else MySQL.</param>
        /// <returns>Returns the delimited table name.</returns>
        public static string ToTableName(this DataRow row, bool isSqlServer = true)
        {
            return isSqlServer ? $"[{row["TABLE_SCHEMA"]}].[{row["TABLE_NAME"]}]" : $"`{row["TABLE_NAME"]}`";
        }

        /// <summary>
        /// Converts Enumeration type into an object of ids and names.
        /// </summary>
        /// <param name="t">Enum type</param>
        public static IEnumerable<object> TranslatedList(this Type t, ResourceDictionary resource = null, string prefix = "")
        {
            if (t == null || !t.IsEnum)
            {
                return null;
            }

            var names = Enum.GetNames(t);
            var values = Enum.GetValues(t);
            if (resource != null)
            {
                return (from i in Enumerable.Range(0, names.Length) select new { Name = resource[prefix + names[i]], Id = (int)values.GetValue(i) }).OrderBy(x => x.Name).ThenBy(x => x.Id);
            }
            return (from i in Enumerable.Range(0, names.Length) select new { Name = names[i], Id = (int)values.GetValue(i) }).OrderBy(x => x.Name).ThenBy(x => x.Id);
        }

        /// <summary>
        /// Converts Enumeration type into an object of ids and names.
        /// </summary>
        /// <param name="t">Enum type</param>
        public static IEnumerable<SelectListItem> TranslatedSelect(this Type t, ResourceDictionary resource = null, string prefix = "")
        {
            if (t == null || !t.IsEnum)
            {
                return null;
            }

            var intValues = Enum.GetValues(t).Cast<int>().ToArray();
            if (resource != null)
            {
                return (from i in intValues select new SelectListItem() { Text = resource[prefix + Enum.GetName(t, i)], Value = i.ToString() }).OrderBy(x => x.Text).ThenBy(x => x.Value);
            }
            return (from i in intValues select new SelectListItem() { Text = Enum.GetName(t, i), Value = i.ToString() }).OrderBy(x => x.Text).ThenBy(x => x.Value);
        }

        /// <summary>
        /// Uppercase the first character of a string.
        /// </summary>
        /// <param name="value">String to update.</param>
        /// <returns>Updated string.</returns>
        public static string UppercaseFirst(this string value)
        {
            return value.IsEmpty() ? string.Empty : (char.ToUpper(value[0]) + value.Substring(1));
        }

        /// <summary>
        /// Get the user ID from the claims.
        /// </summary>
        /// <param name="claimsPrincipal">Claims principal for user.</param>
        /// <returns>UserID if available, else null.</returns>
        public static int UserId(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value.ToInt() ?? 0;
        }

        /// <summary>
        /// Check if user has help enabled.
        /// </summary>
        /// <param name="httpContext">Current request context.</param>
        /// <returns>True if user enabled help, else false.</returns>
        public static bool WantsHelp(this HttpContext httpContext)
        {
            return httpContext.Session.GetString("ContextHelp").ToBool();
        }

        /// <summary>
        /// Get the week of the year for a date.
        /// </summary>
        /// <param name="dt">Date to get value for.</param>
        /// <returns>Week number</returns>
        public static int Week(this DateTime dt)
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
