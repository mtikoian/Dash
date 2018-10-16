using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private Report _Report;

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

        [DbIgnore, JilDirective(true)]
        public bool AllowEdit { get { return UserId == RequestUserId; } }

        [Display(Name = "Chart", ResourceType = typeof(Widgets))]
        public int? ChartId { get; set; }

        [DbIgnore]
        public int DatasetId { get; set; }

        [DbIgnore]
        public string DisplayCurrencyFormat { get; set; }

        [DbIgnore]
        public string DisplayDateFormat { get; set; }

        public int Height { get; set; } = 4;

        [DbIgnore]
        public bool IsData { get { return ReportId.HasPositiveValue(); } }

        [Display(Name = "RefreshRate", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RefreshRate { get; set; }

        [DbIgnore]
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

        [DbIgnore]
        public int ReportRowLimit { get; set; }

        [DbIgnore]
        public decimal ReportWidth { get; set; }

        public Report Report
        {
            get
            {
                if (!ReportId.HasValue)
                {
                    return null;
                }
                if (_Report == null)
                {
                    _Report = DbContext.Get<Report>(ReportId.Value);
                    if (_Report != null)
                    {
                        _Report.IsDashboard = true;
                    }
                }
                return _Report;
            }
        }

        [Display(Name = "Title", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [DbIgnore]
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

        [DbIgnore]
        public DateTimeOffset WidgetDateUpdated { get; set; }

        public int Width { get; set; } = 4;

        public int X { get; set; } = -1;

        public int Y { get; set; } = -1;
    }
}
