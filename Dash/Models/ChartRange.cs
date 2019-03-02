using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class ChartRange : BaseModel
    {
        private Chart _Chart;
        private Report _Report;
        private DatasetColumn _XAxisColumn;
        private DatasetColumn _YAxisColumn;

        public ChartRange()
        {
        }

        public ChartRange(IDbContext dbContext) => DbContext = dbContext;

        public ChartRange(IDbContext dbContext, int chartId)
        {
            DbContext = dbContext;
            ChartId = chartId;
        }

        [Display(Name = "Aggregator", ResourceType = typeof(Charts))]
        public int AggregatorId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> AggregatorListItems => typeof(Aggregators).TranslatedSelect(new ResourceDictionary("Filters"), "LabelGroup_");

        public Chart Chart => _Chart ?? (_Chart = DbContext.Get<Chart>(ChartId));

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ChartName => Chart?.Name;

        [Display(Name = "Color", ResourceType = typeof(Charts))]
        public string Color { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> ColumnSelectListItems => Report?.Dataset?.DatasetColumn.OrderBy(x => x.Title).ToSelectList(x => x.Title, x => x.Id.ToString());

        [Display(Name = "DateInterval", ResourceType = typeof(Charts))]
        public int? DateIntervalId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> DateIntervalSelectListItems => typeof(DateIntervals).TranslatedSelect(new ResourceDictionary("Filters"), "LabelDateInterval_");

        public int DisplayOrder { get; set; }

        [Display(Name = "FillDateGaps", ResourceType = typeof(Charts))]
        public bool FillDateGaps { get; set; }

        [DbIgnore]
        public bool IsLast { get; set; }

        public Report Report => _Report ?? (_Report = DbContext.Get<Report>(ReportId));

        [Display(Name = "Report", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName => Report?.Name;

        [DbIgnore, BindNever, ValidateNever]
        public IEnumerable<SelectListItem> ReportSelectListItems => DbContext.GetAll<Report>(new { UserId = RequestUserId }).ToSelectList(x => x.Name, x => x.Id.ToString());

        [DbIgnore, BindNever, ValidateNever]
        public DatasetColumn XAxisColumn => _XAxisColumn ?? (_XAxisColumn = DbContext.Get<DatasetColumn>(XAxisColumnId));

        [Display(Name = "XAxisColumn", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int XAxisColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string XAxisColumnName => XAxisColumn?.Title;

        [DbIgnore, BindNever, ValidateNever]
        public DatasetColumn YAxisColumn => _YAxisColumn ?? (_YAxisColumn = DbContext.Get<DatasetColumn>(YAxisColumnId));

        [Display(Name = "YAxisColumn", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int YAxisColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string YAxisColumnName => YAxisColumn?.Title;

        public bool MoveDown(out string error)
        {
            error = "";
            var ranges = DbContext.GetAll<ChartRange>(new { ChartId }).ToList();
            if (DisplayOrder == ranges.Count - 1)
            {
                // can't move any higher
                error = Charts.ErrorAlreadyLastRange;
                return false;
            }
            var range = ranges.First(x => x.DisplayOrder == DisplayOrder + 1);
            DbContext.WithTransaction(() => {
                range.DisplayOrder--;
                DbContext.Save(range);

                DisplayOrder++;
                DbContext.Save(this);
            });
            return true;
        }

        public bool MoveUp(out string error)
        {
            error = "";
            if (DisplayOrder == 0)
            {
                // can't move any lower
                error = Charts.ErrorAlreadyFirstRange;
                return false;
            }
            var range = DbContext.GetAll<ChartRange>(new { ChartId }).First(x => x.DisplayOrder == DisplayOrder - 1);
            range.DisplayOrder++;
            DbContext.WithTransaction(() => {
                DbContext.Save(range);
                DisplayOrder--;
                DbContext.Save(this);
            });
            return true;
        }
    }
}
