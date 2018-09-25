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
    public class ChartRange : BaseModel
    {
        private Chart _Chart;
        private Report _Report;
        private DatasetColumn _XAxisColumn;
        private DatasetColumn _YAxisColumn;

        public ChartRange()
        {
        }

        public ChartRange(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ChartRange(IDbContext dbContext, int chartId)
        {
            DbContext = dbContext;
            ChartId = chartId;
        }

        [Display(Name = "Aggregator", ResourceType = typeof(Charts))]
        public int AggregatorId { get; set; }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public IEnumerable<SelectListItem> AggregatorSelectListItems
        {
            get
            {
                return typeof(Aggregators).TranslatedSelect(new ResourceDictionary("Charts"), "LabelAggregator_");
            }
        }

        [Ignore]
        public bool IsLast { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        [Display(Name = "Color", ResourceType = typeof(Charts))]
        public string Color { get; set; }

        [Display(Name = "DateInterval", ResourceType = typeof(Charts))]
        public int? DateIntervalId { get; set; }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public IEnumerable<SelectListItem> DateIntervalSelectListItems
        {
            get
            {
                return typeof(DateIntervals).TranslatedSelect(new ResourceDictionary("Filters"), "LabelDateInterval_");
            }
        }

        public int DisplayOrder { get; set; }

        [Display(Name = "Report", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [Ignore, BindNever, ValidateNever]
        public string ChartName { get { return Chart?.Name; } }

        [Ignore, BindNever, ValidateNever]
        public string ReportName { get { return Report?.Name; } }

        [Ignore, BindNever, ValidateNever]
        public string XAxisColumnName { get { return XAxisColumn?.Title; } }

        [Ignore, BindNever, ValidateNever]
        public string YAxisColumnName { get { return YAxisColumn?.Title; } }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public IEnumerable<SelectListItem> ReportSelectListItems
        {
            get
            {
                return DbContext.GetAll<Report>(new { UserId = RequestUserId }).ToSelectList(x => x.Name, x => x.Id.ToString());
            }
        }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public IEnumerable<SelectListItem> ColumnSelectListItems
        {
            get
            {
                return Report?.Dataset?.DatasetColumn.OrderBy(x => x.Title).ToSelectList(x => x.Title, x => x.Id.ToString());
            }
        }

        [Display(Name = "XAxisColumn", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int XAxisColumnId { get; set; }

        [Display(Name = "YAxisColumn", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int YAxisColumnId { get; set; }

        private Chart Chart { get { return _Chart ?? (_Chart = DbContext.Get<Chart>(ChartId)); } }

        private Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(ReportId)); } }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public DatasetColumn XAxisColumn { get { return _XAxisColumn ?? (_XAxisColumn = DbContext.Get<DatasetColumn>(XAxisColumnId)); } }

        [Ignore, BindNever, ValidateNever, JilDirective(true)]
        public DatasetColumn YAxisColumn { get { return _YAxisColumn ?? (_YAxisColumn = DbContext.Get<DatasetColumn>(YAxisColumnId)); } }

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
