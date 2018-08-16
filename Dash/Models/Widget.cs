using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Jil;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class Widget : BaseModel, IValidatableObject
    {
        public Widget()
        {
        }

        public Widget(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [Ignore, JilDirective(true)]
        public bool AllowEdit { get { return UserId == RequestUserId; } }

        [Display(Name = "Chart", ResourceType = typeof(Widgets))]
        public int? ChartId { get; set; }

        public int Height { get; set; } = 4;

        [Display(Name = "RefreshRate", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RefreshRate { get; set; }

        [Display(Name = "Report", ResourceType = typeof(Widgets))]
        public int? ReportId { get; set; }

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

        public IEnumerable<SelectListItem> GetChartSelectList()
        {
            return DbContext.GetAll<Chart>(new { UserId = RequestUserId }).ToSelectList(r => r.Name, r => r.Id.ToString());
        }

        public IEnumerable<SelectListItem> GetReportSelectList()
        {
            return DbContext.GetAll<Report>(new { UserId = RequestUserId }).ToSelectList(r => r.Name, r => r.Id.ToString());
        }

        public IEnumerable<SelectListItem> GetWidgetRefreshRateSelectList()
        {
            return typeof(WidgetRefreshRates).TranslatedSelect(new ResourceDictionary("Widgets"), "LabelRefreshRate_");
        }

        public void Save()
        {
            RequestUserId = RequestUserId.HasPositiveValue() ? RequestUserId : UserId;

            if (Id == 0)
            {
                // new widget - find the correct position
                var gridBottom = 0;
                DbContext.GetAll<Widget>(new { UserId = RequestUserId }).ToList().ForEach(x => gridBottom = Math.Max(x.Y + x.Height, gridBottom));
                X = 0;
                Y = gridBottom;
            }
            if (UserId == 0)
            {
                UserId = RequestUserId ?? 0;
            }
            DbContext.Save(this);
        }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ReportId.HasPositiveValue() && !ChartId.HasPositiveValue())
            {
                yield return new ValidationResult(Widgets.ErrorReportOrChartRequired, new[] { "ReportId" });
            }
        }
    }
}
