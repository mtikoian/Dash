using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class ReportGroup : BaseModel
    {
        DatasetColumn _Column;
        Report _Report;

        public ReportGroup() { }

        public ReportGroup(IDbContext dbContext) => DbContext = dbContext;

        public ReportGroup(IDbContext dbContext, int reportId)
        {
            DbContext = dbContext;
            ReportId = reportId;
        }

        [DbIgnore, BindNever, ValidateNever]
        public DatasetColumn Column => _Column ?? (_Column = DbContext.Get<DatasetColumn>(ColumnId));

        [Display(Name = "GroupColumn", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ColumnName => Column?.Title;

        [BindNever, ValidateNever]
        public List<DatasetColumn> Columns => Report.Dataset.IsProc ? Report.Dataset.DatasetColumn.Where(x => x.IsParam).ToList() : Report.Dataset.DatasetColumn;

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [DbIgnore]
        public bool IsLast { get; set; }

        [BindNever, ValidateNever]
        public Report Report
        {
            get => _Report ?? (_Report = DbContext.Get<Report>(ReportId));
            set => _Report = value;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName => Report?.Name;

        public bool MoveDown(out string error)
        {
            error = "";
            var groups = DbContext.GetAll<ReportGroup>(new { ReportId }).ToList();
            if (DisplayOrder == groups.Count - 1)
            {
                // can't move any higher
                error = Reports.ErrorAlreadyLastGroup;
                return false;
            }
            var group = groups.First(x => x.DisplayOrder == DisplayOrder + 1);
            DbContext.WithTransaction(() => {
                group.DisplayOrder--;
                DbContext.Save(group);

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
                error = Reports.ErrorAlreadyFirstGroup;
                return false;
            }
            var group = DbContext.GetAll<ReportGroup>(new { ReportId }).First(x => x.DisplayOrder == DisplayOrder - 1);
            group.DisplayOrder++;
            DbContext.WithTransaction(() => {
                DbContext.Save(group);
                DisplayOrder--;
                DbContext.Save(this);
            });
            return true;
        }
    }
}
