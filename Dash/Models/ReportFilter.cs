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
    public class ReportFilter : BaseModel
    {
        private DatasetColumn _Column;
        private Report _Report;

        public ReportFilter()
        {
        }

        public ReportFilter(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ReportFilter(IDbContext dbContext, int reportId)
        {
            DbContext = dbContext;
            ReportId = reportId;
        }

        [DbIgnore, BindNever, ValidateNever, JilDirective(true)]
        public static IEnumerable<SelectListItem> BooleanSelectListItems
        {
            get
            {
                return new List<SelectListItem> {
                    new SelectListItem(Reports.True, "1"),
                    new SelectListItem(Reports.False, "0")
                };
            }
        }

        [DbIgnore, BindNever, ValidateNever, JilDirective(true)]
        public static IEnumerable<SelectListItem> DateIntervalSelectListItems
        {
            get { return typeof(FilterDateRanges).TranslatedSelect(new ResourceDictionary("Filters"), "LabelDateRange_"); }
        }

        [DbIgnore, BindNever, ValidateNever, JilDirective(true)]
        public DatasetColumn Column { get { return _Column ?? (_Column = DbContext.Get<DatasetColumn>(ColumnId)); } }

        [Display(Name = "FilterColumn", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ColumnName
        {
            get { return Column?.Title; }
        }

        [BindNever, ValidateNever]
        public List<DatasetColumn> Columns
        {
            get { return Report?.Dataset?.DatasetColumn; }
        }

        [Display(Name = "FilterCriteria", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria { get; set; }

        [Display(Name = "FilterCriteria2", ResourceType = typeof(Reports))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria2 { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string CriteriaValue
        {
            get
            {
                if (Column.FilterTypeId == (int)FilterTypes.Boolean)
                {
                    return Criteria == "1" ? Reports.True : Reports.False;
                }
                if (OperatorId == (int)FilterOperatorsAbstract.DateInterval)
                {
                    var key = ((FilterDateRanges)Criteria.ToInt()).ToString();
                    var resx = new ResourceDictionary("Filters");
                    resx.Dictionary.TryGetValue($"LabelDateRange_{key}", out var value);
                    return value.IsEmpty() ? key : value;
                }
                if (Column.FilterTypeId == (int)FilterTypes.Select)
                {
                    return FilterSelectListItems.FirstOrDefault(x => x.Value == Criteria)?.Text;
                }
                return Criteria;
            }
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [BindNever, ValidateNever, JilDirective(true)]
        public IEnumerable<SelectListItem> FilterSelectListItems
        {
            get
            {
                if (!Column.IsSelect || Column.FilterQuery.IsEmpty())
                {
                    return new List<SelectListItem>();
                }
                return Report.Dataset.Database.Query<LookupItem>(Column.FilterQuery)
                    .Prepend(new LookupItem { Value = "", Text = Reports.FilterCriteria })
                    .Select(x => new SelectListItem("Text", "Value"));
            }
        }

        [DbIgnore]
        public bool IsLast { get; set; }

        [Display(Name = "FilterOperator", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int OperatorId { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> OperatorSelectListItems
        {
            get
            {
                var filterResource = new ResourceDictionary("Filters");
                Type operatorType;
                switch (Column?.FilterTypeId)
                {
                    case ((int)FilterTypes.Boolean):
                        operatorType = typeof(FilterOperatorsBoolean);
                        break;
                    case ((int)FilterTypes.Date):
                        operatorType = typeof(FilterOperatorsDate);
                        break;
                    case ((int)FilterTypes.Numeric):
                        operatorType = typeof(FilterOperatorsNumeric);
                        break;
                    case ((int)FilterTypes.Select):
                        operatorType = typeof(FilterOperatorsSelect);
                        break;
                    default:
                        operatorType = typeof(FilterOperatorsText);
                        break;
                }
                return operatorType.TranslatedSelect(filterResource, "LabelFilter_");
            }
        }

        [DbIgnore, BindNever, ValidateNever]
        public string OperatorValue
        {
            get
            {
                var key = ((FilterOperatorsAbstract)OperatorId).ToString();
                var resx = new ResourceDictionary("Filters");
                resx.Dictionary.TryGetValue($"LabelFilter_{key}", out var value);
                return value.IsEmpty() ? key : value;
            }
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName { get { return Report?.Name; } }

        private Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(ReportId)); } }

        public string BuildFilterSql(DatasetColumn column, ReportFilter filter, out Dictionary<string, object> parameters)
        {
            var sql = "";
            var alias1 = column.IsParam ? column.ColumnName : $"@{column.Alias}_{filter.Id}";
            var alias2 = $"@end_{column.Alias}_{filter.Id}";
            parameters = new Dictionary<string, object>();

            if (Criteria2 != null)
            {
                parameters.Add(alias2, column.IsDateTime ? Criteria2.ToDateTime().ToSqlDateTime() : Criteria2);
            }
            parameters.Add(alias1, column.IsDateTime ? Criteria.ToDateTime().ToSqlDateTime() : Criteria);

            switch ((FilterOperatorsAbstract)OperatorId)
            {
                case FilterOperatorsAbstract.Equal:
                    sql = column.IsBool ? (Criteria.ToBool() ? "= 1" : "!= 1") : "= " + alias1;
                    break;
                case FilterOperatorsAbstract.NotEqual:
                    sql = column.IsBool ? (Criteria.ToBool() ? "!= 1" : "= 1") : "!= " + alias1;
                    break;
                case FilterOperatorsAbstract.GreaterThan:
                    sql = "> " + alias1;
                    break;
                case FilterOperatorsAbstract.LessThan:
                    sql = "< " + alias1;
                    break;
                case FilterOperatorsAbstract.GreaterThanEqualTo:
                    sql = ">= " + alias1;
                    break;
                case FilterOperatorsAbstract.LessThanEqualTo:
                    sql = "<= " + alias1;
                    break;
                case FilterOperatorsAbstract.Range:
                    if (column.IsDateTime)
                    {
                        var start = Criteria.ToDateTime();
                        var end = Criteria2.ToDateTime();
                        parameters[alias1] = (start > end ? end : start).ToSqlDateTime();
                        parameters[alias2] = (start > end ? start : end).ToSqlDateTime();
                    }
                    else
                    {
                        var start = Criteria.ToDouble();
                        var end = Criteria2.ToDouble();
                        parameters[alias1] = Math.Min(start, end);
                        parameters[alias2] = Math.Max(start, end);
                    }
                    sql = $"BETWEEN {alias1} AND {alias2}";
                    break;
                case FilterOperatorsAbstract.In:
                case FilterOperatorsAbstract.NotIn:
                    parameters.Remove(alias1);
                    sql = (FilterOperatorsAbstract)OperatorId == FilterOperatorsAbstract.In ? "IN (" : "NOT IN (";
                    var list = Criteria.Delimit();
                    for (var i = 0; i < list.Count; i++)
                    {
                        sql += alias1 + "_" + i + (list.Count > i + 1 ? ", " : "");
                        parameters[alias1 + "_" + i] = list[i];
                    }
                    sql += ")";
                    break;
                case FilterOperatorsAbstract.Like:
                    sql = $"LIKE {alias1}";
                    parameters[alias1] = $"%{parameters[alias1]}%";
                    break;
                case FilterOperatorsAbstract.NotLike:
                    sql = $"NOT LIKE {alias1}";
                    parameters[alias1] = $"%{parameters[alias1]}%";
                    break;
                case FilterOperatorsAbstract.DateInterval:
                    // handle special date functions
                    var today = DateTime.Today;
                    var startDate = today;
                    var endDate = today;

                    switch ((FilterDateRanges)Criteria.ToInt())
                    {
                        case FilterDateRanges.Today:
                            endDate = today.AddDays(1).AddMilliseconds(-1);
                            break;
                        case FilterDateRanges.ThisWeek:
                            startDate = today.StartOfWeek();
                            endDate = today.EndOfWeek();
                            break;
                        case FilterDateRanges.ThisMonth:
                            startDate = today.StartOfMonth();
                            endDate = today.EndOfMonth();
                            break;
                        case FilterDateRanges.ThisQuarter:
                            startDate = today.StartOfQuarter();
                            endDate = today.EndOfQuarter();
                            break;
                        case FilterDateRanges.ThisYear:
                            startDate = today.StartOfYear();
                            endDate = today.EndOfYear();
                            break;
                        case FilterDateRanges.Yesterday:
                            startDate = today.AddDays(-1);
                            endDate = today.AddMilliseconds(-1);
                            break;
                        case FilterDateRanges.LastWeek:
                            var w = today.AddDays(-7);
                            startDate = today.AddDays(-7).StartOfWeek();
                            endDate = startDate.EndOfWeek();
                            break;
                        case FilterDateRanges.LastMonth:
                            startDate = today.AddMonths(-1).StartOfMonth();
                            endDate = startDate.EndOfMonth();
                            break;
                        case FilterDateRanges.LastQuarter:
                            startDate = today.AddMonths(-3).StartOfQuarter();
                            endDate = startDate.EndOfQuarter();
                            break;
                        case FilterDateRanges.LastYear:
                            startDate = today.AddYears(-1).StartOfYear();
                            endDate = startDate.EndOfYear();
                            break;
                    }

                    parameters[alias1] = startDate.ToSqlDateTime();
                    parameters[alias2] = endDate.ToSqlDateTime();
                    sql = $"BETWEEN {alias1} AND {alias2}";
                    break;
            }

            return $"({column.BuildSql(false)} {sql})";
        }

        public bool MoveDown(out string error)
        {
            error = "";
            var filters = DbContext.GetAll<ReportFilter>(new { ReportId }).ToList();
            if (DisplayOrder == filters.Count - 1)
            {
                // can't move any higher
                error = Reports.ErrorAlreadyLastFilter;
                return false;
            }
            var filter = filters.First(x => x.DisplayOrder == DisplayOrder + 1);
            DbContext.WithTransaction(() => {
                filter.DisplayOrder--;
                DbContext.Save(filter);

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
                error = Reports.ErrorAlreadyFirstFilter;
                return false;
            }
            var filter = DbContext.GetAll<ReportFilter>(new { ReportId }).First(x => x.DisplayOrder == DisplayOrder - 1);
            filter.DisplayOrder++;
            DbContext.WithTransaction(() => {
                DbContext.Save(filter);
                DisplayOrder--;
                DbContext.Save(this);
            });
            return true;
        }
    }
}
