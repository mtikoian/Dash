using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Dash.TagHelpers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

        public IActionContextAccessor ActionContextAccessor { get; set; }

        [DbIgnore]
        public bool AllowEdit => UserId == RequestUserId;

        [Display(Name = "Chart", ResourceType = typeof(Widgets))]
        public int? ChartId { get; set; }

        [DbIgnore]
        public int DatasetId { get; set; }

        [DbIgnore]
        public string DisplayCurrencyFormat { get; set; }

        [DbIgnore]
        public string DisplayDateFormat { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<DropdownListItem> DropDownItems => new List<DropdownListItem> {
            new DropdownListItem { Label = Widgets.Refresh, ExtraClasses = "btn btn-link btn-refresh", Icon = DashIcons.ArrowsCw },
            new DropdownListItem { Label = Widgets.ToggleFullScreen, ExtraClasses = "btn btn-link btn-fullscreen", Icon = DashIcons.Max },
            (ChartId.HasPositiveValue() ? new DropdownListItem { Label = Widgets.ViewChart, Controller = "Chart", Action = "Edit", RouteValues = new { Id = ChartId }, ExtraClasses = "btn btn-link fs-disabled", Icon = DashIcons.Info } :
                new DropdownListItem { Label = Widgets.ViewReport, Controller = "Report", Action = "Edit", RouteValues = new { Id = ReportId }, ExtraClasses = "btn btn-link fs-disabled", Icon = DashIcons.Info }),
            new DropdownListItem { Label = Widgets.EditWidget, Controller = "Dashboard", Action = "Edit", RouteValues = new { Id = Id }, ExtraClasses = "btn btn-link fs-disabled", Icon = DashIcons.Pencil },
            new DropdownListItem { Label = Widgets.DeleteWidget, Controller = "Dashboard", Action = "Delete", RouteValues = new { Id = Id }, Method = HttpVerbs.Delete,
                ExtraClasses = "btn btn-link fs-disabled", Icon = DashIcons.Trash, IconExtraClasses = "text-error", Confirm = Core.AreYouSure
            }
        };

        public int Height { get; set; } = 4;

        [DbIgnore]
        public bool IsData => ReportId.HasPositiveValue();

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

        [Display(Name = "Report", ResourceType = typeof(Widgets))]
        public int? ReportId { get; set; }

        [DbIgnore]
        public int ReportRowLimit { get; set; }

        [DbIgnore]
        public decimal ReportWidth { get; set; }

        [Display(Name = "Title", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [Display(Name = "User", ResourceType = typeof(Widgets))]
        [Required]
        public int UserId { get; set; }

        public int Width { get; set; } = 4;

        public int X { get; set; } = -1;

        public int Y { get; set; } = -1;
    }
}
