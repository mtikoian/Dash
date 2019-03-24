using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Jil;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class Widget : BaseModel, IValidatableObject
    {
        public Widget() { }

        public Widget(IDbContext dbContext) => DbContext = dbContext;

        [Display(Name = "Chart", ResourceType = typeof(Widgets))]
        public int? ChartId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> ChartSelectListItems => DbContext.GetAll<Chart>(new { UserId = RequestUserId }).ToSelectList(r => r.Name, r => r.Id.ToString());
        public int Height { get; set; } = 4;

        [DbIgnore, BindNever, ValidateNever, JilDirective(true)]
        public bool IsOwner
        {
            get
            {
                if (UserCreated == 0 && Id > 0)
                    UserCreated = DbContext.Get<Widget>(Id)?.UserCreated ?? 0;
                return RequestUserId == UserCreated;
            }
        }

        [Display(Name = "RefreshRate", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int RefreshRate { get; set; }

        [Display(Name = "Report", ResourceType = typeof(Widgets))]
        public int? ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> ReportSelectListItems => DbContext.GetAll<Report>(new { UserId = RequestUserId }).ToSelectList(r => r.Name, r => r.Id.ToString());

        [Display(Name = "Title", ResourceType = typeof(Widgets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Title { get; set; }

        [DbIgnore, JilDirective(true)]
        public int UserCreated { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> WidgetRefreshRateSelectListItems => typeof(WidgetRefreshRates).TranslatedSelect(new ResourceDictionary("Widgets"), "LabelRefreshRate_");
        public int Width { get; set; } = 4;

        public int X { get; set; } = -1;

        public int Y { get; set; } = -1;

        public void Save()
        {
            if (Id == 0)
            {
                // new widget - find the correct position
                var gridBottom = 0;
                DbContext.GetAll<Widget>(new { UserId = RequestUserId }).ToList().ForEach(x => gridBottom = Math.Max(x.Y + x.Height, gridBottom));
                X = 0;
                Y = gridBottom;
            }
            DbContext.Save(this);
        }

        public void SavePosition(int width, int height, int x, int y)
        {
            if (!IsOwner)
                return;

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
                yield return new ValidationResult(Widgets.ErrorReportOrChartRequired, new[] { "ReportId" });
        }
    }
}
