using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
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

        [Display(Name = "FilterColumn", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [Ignore]
        [BindNever, ValidateNever]
        public string ColumnName
        {
            get
            {
                if (_Column == null)
                {
                    _Column = DbContext.Get<DatasetColumn>(ColumnId);
                }
                return _Column?.Title;
            }
        }

        [Display(Name = "FilterCriteria", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria { get; set; }

        [Display(Name = "FilterCriteria2", ResourceType = typeof(Reports))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria2 { get; set; }

        [Ignore]
        public string[] CriteriaJson { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [Ignore]
        public bool IsLast { get; set; }

        [Display(Name = "FilterOperator", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int OperatorId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [Ignore]
        [BindNever, ValidateNever]
        public string ReportName { get { return Report?.Name; } }

        [BindNever, ValidateNever]
        private Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(ReportId)); } }

        [BindNever, ValidateNever]
        public List<DatasetColumn> Columns
        {
            get
            {
                return Report?.Dataset?.DatasetColumn;
            }
        }

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

        public bool IsDate()
        {
            return OperatorId == (int)FilterOperatorsAbstract.DateInterval;
        }

        public bool IsRange()
        {
            return OperatorId == (int)FilterOperatorsAbstract.Range;
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
                return this;
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
                return this;
            });
            return true;
        }
    }
}
