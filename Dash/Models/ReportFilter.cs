using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class ReportFilter : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria { get; set; }

        [StringLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria2 { get; set; }

        [Ignore]
        public string[] CriteriaJson { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int OperatorId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

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
    }
}
