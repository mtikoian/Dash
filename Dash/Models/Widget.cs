using Dash.I18n;
using Jil;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dash.Models
{
    public enum WidgetRefreshRates
    {
        Never = 0,
        ThirtySeconds = 1,
        OneMinute = 2,
        FiveMinutes = 3,
        TenMinutes = 4,
        FifteenMinutes = 5,
        ThirtyMinutes = 6,
        OneHour = 7
    }

    /// <summary>
    /// Widgets is an element on the user dashboard that shows data/chart from a report.
    /// </summary>
    public class Widget : BaseModel, IValidatableObject
    {
        private List<Column> _Columns;
        private List<DatasetColumn> _DatasetColumns;
        private IActionContextAccessor ActionContextAccessor;

        public Widget(IActionContextAccessor actionContextAccessor)
        {
            ActionContextAccessor = actionContextAccessor;
        }

        [Ignore, JilDirective(true)]
        public bool AllowEdit { get { return UserId == Authorization.User?.Id; } }

        [Display(Name = "Chart", ResourceType = typeof(I18n.Widgets))]
        public int? ChartId { get; set; }

        [Ignore]
        public IEnumerable<object> Columns
        {
            get
            {
                if (_Columns == null && ReportId.HasPositiveValue())
                {
                    _Columns = DbContext.Query<Column>("ColumnGetForReport", new { ReportId }).ToList();
                }
                // may want to rework how i query columns since i have to make another query for datasetcolumns anyway
                if (_DatasetColumns == null && DatasetId > 0)
                {
                    _DatasetColumns = DbContext.GetAll<DatasetColumn>(new { DatasetId }).ToList();
                }

                return _Columns?.Any() == true ? _Columns.Select(c => {
                    var link = c.Link;
                    if (!link.IsEmpty())
                    {
                        _DatasetColumns.ForEach(dc => link = link.ReplaceCase(dc.ColumnName, String.Format("{{{0}}}", dc.Alias)));
                    }
                    return new {
                        Field = c.Alias ?? "",
                        Label = c.Title,
                        Sortable = true,
                        DataType = c.TableDataType,
                        Width = c.Width,
                        Links = new List<TableLink>().AddLink(link, Html.Classes().Append("target", "_blank"), render: !link.IsEmpty())
                    };
                }) : null;
            }
        }

        [Ignore]
        public int DatasetId { get; set; }

        [Ignore]
        public string DisplayCurrencyFormat { get; set; }

        [Ignore]
        public string DisplayDateFormat { get; set; }

        public int Height { get; set; } = 4;

        [Ignore]
        public bool IsData { get { return ReportId.HasPositiveValue(); } }

        [Display(Name = "RefreshRate", ResourceType = typeof(I18n.Widgets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int RefreshRate { get; set; }

        [Ignore]
        public int RefreshSeconds
        {
            get
            {
                switch ((WidgetRefreshRates)RefreshRate)
                {
                    case WidgetRefreshRates.ThirtySeconds:
                        return 30;

                    case WidgetRefreshRates.OneMinute:
                        return 60;

                    case WidgetRefreshRates.FiveMinutes:
                        return 300;

                    case WidgetRefreshRates.TenMinutes:
                        return 600;

                    case WidgetRefreshRates.FifteenMinutes:
                        return 900;

                    case WidgetRefreshRates.ThirtyMinutes:
                        return 1800;

                    case WidgetRefreshRates.OneHour:
                        return 3600;

                    default:
                        return 0;
                }
            }
        }

        [Display(Name = "Report", ResourceType = typeof(I18n.Widgets))]
        public int? ReportId { get; set; }

        [Ignore]
        public int ReportRowLimit { get; set; }

        [Ignore]
        public decimal ReportWidth { get; set; }

        [Ignore]
        public IEnumerable<TableSorting> SortColumns
        {
            get
            {
                if (_Columns == null && ReportId.HasPositiveValue())
                {
                    _Columns = DbContext.Query<Column>("ColumnGetForReport", new { ReportId }).ToList();
                }
                return _Columns?.Any() == true ? _Columns.Where(c => c.SortOrder > 0).OrderBy(c => c.SortOrder).Select(c => new TableSorting {
                    Field = c.Alias,
                    Dir = c.SortDirection,
                    Index = c.SortOrder + 1,
                    DataType = c.TableDataType.ToString()
                }) : null;
            }
        }

        [Display(Name = "Title", ResourceType = typeof(I18n.Widgets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [Ignore]
        public string Url
        {
            get
            {
                return new UrlHelper(ActionContextAccessor.ActionContext)
                    .Action("Data", IsData ? "Report" : "Chart", new RouteValueDictionary(new { @id = IsData ? ReportId : ChartId }));
            }
        }

        [Display(Name = "User", ResourceType = typeof(I18n.Widgets))]
        [Required]
        [JilDirective(true)]
        public int UserId { get; set; } = Authorization.User.Id;

        [Ignore]
        public DateTimeOffset WidgetDateUpdated { get; set; }

        public int Width { get; set; } = 4;

        public int X { get; set; } = -1;

        public int Y { get; set; } = -1;

        /// <summary>
        /// Get all the charts available to the user as a select list.
        /// </summary>
        /// <returns>Returns a IEnumerable of list items.</returns>
        public IEnumerable<SelectListItem> GetChartSelectList()
        {
            return DbContext.GetAll<Chart>(new { UserId = Authorization.User.Id }).ToSelectList(r => r.Name, r => r.Id.ToString());
        }

        /// <summary>
        /// Get all the reports available to the user as a select list.
        /// </summary>
        /// <returns>Returns a IEnumerable of list items.</returns>
        public IEnumerable<SelectListItem> GetReportSelectList()
        {
            return DbContext.GetAll<Report>(new { UserId = Authorization.User.Id }).ToSelectList(r => r.Name, r => r.Id.ToString());
        }

        /// <summary>
        /// Get all the widget refresh rates as a select list.
        /// </summary>
        /// <returns>Returns a IEnumerable of list items.</returns>
        public IEnumerable<SelectListItem> GetWidgetRefreshRateSelectList()
        {
            return typeof(WidgetRefreshRates).TranslatedSelect(new ResourceDictionary("Widgets"), "LabelRefreshRate_");
        }

        /// <summary>
        /// Save the widget, calculating the position for the widget first if it is new.
        /// </summary>
        /// <returns>Returns tru if save is successful, else false.</returns>
        public void Save()
        {
            if (Id == 0)
            {
                // new widget - find the correct position
                var gridBottom = 0;
                DbContext.GetAll<Widget>(new { UserId = Authorization.User.Id }).ToList().ForEach(x => gridBottom = Math.Max(x.Y + x.Height, gridBottom));
                X = 0;
                Y = gridBottom;
            }
            DbContext.Save(this);
        }

        /// <summary>
        /// Save widgets size and position to db if changed.
        /// </summary>
        /// <param name="width">Width of widget.</param>
        /// <param name="height">Height of widget.</param>
        /// <param name="x">X coordinate of widget.</param>
        /// <param name="y">Y coordinate of widget.</param>
        public void SavePosition(int width, int height, int x, int y)
        {
            if (!AllowEdit)
            {
                return;
            }

            width = width == 0 ? 1 : width;
            height = height == 0 ? 1 : height;
            var changed = width != Width || height != Height || x != X || y != Y;
            if (changed)
            {
                Width = width;
                Height = height;
                X = x;
                Y = y;
                Save();
            }
        }

        /// <summary>
        /// Validate widget object. Check that report or chart one is provided.
        /// </summary>
        /// <returns>Returns a list of errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ReportId.HasPositiveValue() && !ChartId.HasPositiveValue())
            {
                yield return new ValidationResult(I18n.Widgets.ErrorReportOrChartRequired, new[] { "ReportId" });
            }
        }
    }
}