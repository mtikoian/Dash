using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Jil;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

    public class WidgetView : BaseModel
    {
        private List<Column> _Columns;
        private List<DatasetColumn> _DatasetColumns;

        public WidgetView()
        {
        }

        public WidgetView(IDbContext dbContext, IActionContextAccessor actionContextAccessor)
        {
            DbContext = dbContext;
            ActionContextAccessor = actionContextAccessor;
            UserId = ActionContextAccessor.ActionContext.HttpContext.User.UserId();
        }

        [JilDirective(true)]
        public IActionContextAccessor ActionContextAccessor { get; set; }

        [Ignore, JilDirective(true)]
        public bool AllowEdit { get { return UserId == RequestUserId; } }

        [Display(Name = "Chart", ResourceType = typeof(Widgets))]
        public int? ChartId { get; set; }

        [Ignore]
        [BindNever, ValidateNever]
        public IEnumerable<object> Columns
        {
            get
            {
                if (_Columns == null && ReportId.HasPositiveValue())
                {
                    _Columns = DbContext.Query<Column>("ColumnGetForReport", new { ReportId })
                        .Each(x => x.DbContext = DbContext).ToList();
                }
                if (ReportId.HasPositiveValue() && DatasetId == 0)
                {
                    DatasetId = DbContext.Get<Report>(ReportId.Value)?.DatasetId ?? 0;
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
                        Links = new List<TableLink>().AddIf(new TableLink(link, Html.Classes().Append("target", "_blank")), !link.IsEmpty())
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

        [Display(Name = "RefreshRate", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
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

        [Display(Name = "Report", ResourceType = typeof(Widgets))]
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

        [Display(Name = "Title", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [Ignore]
        [BindNever, ValidateNever]
        public string Url
        {
            get
            {
                return new UrlHelper(ActionContextAccessor.ActionContext)
                    .Action("Data", IsData ? "Report" : "Chart", new RouteValueDictionary(new { @id = IsData ? ReportId : ChartId }));
            }
        }

        [Display(Name = "User", ResourceType = typeof(Widgets))]
        [Required]
        [JilDirective(true)]
        public int UserId { get; set; }

        [Ignore]
        public DateTimeOffset WidgetDateUpdated { get; set; }

        public int Width { get; set; } = 4;

        public int X { get; set; } = -1;

        public int Y { get; set; } = -1;

    }
}
