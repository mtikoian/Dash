using System;
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
    public class ReportFilter : BaseModel, IValidatableObject
    {
        DatasetColumn _Column;
        Report _Report;
        List<ReportFilterCriteria> _ReportFilterCriteria;

        public ReportFilter() { }

        public ReportFilter(IDbContext dbContext) => DbContext = dbContext;

        public ReportFilter(IDbContext dbContext, int reportId)
        {
            DbContext = dbContext;
            ReportId = reportId;
        }

        [DbIgnore, BindNever, ValidateNever]
        public static IEnumerable<SelectListItem> BooleanSelectListItems => new List<SelectListItem> { new SelectListItem(Reports.True, "1"), new SelectListItem(Reports.False, "0") };

        [DbIgnore, BindNever, ValidateNever]
        public static IEnumerable<SelectListItem> DateIntervalSelectListItems => typeof(FilterDateRanges).TranslatedSelect(new ResourceDictionary("Filters"), "LabelDateRange_");

        [DbIgnore, BindNever, ValidateNever]
        public DatasetColumn Column => _Column ?? (_Column = DbContext.Get<DatasetColumn>(ColumnId));

        [Display(Name = "FilterColumn", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ColumnId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ColumnName => Column?.Title;

        [BindNever, ValidateNever]
        public List<DatasetColumn> Columns => Report.Dataset.IsProc ? Report.Dataset.DatasetColumn.Where(x => x.IsParam).ToList() : Report.Dataset.DatasetColumn;

        [Display(Name = "FilterCriteria", ResourceType = typeof(Reports))]
        [StringLength(4000, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria { get; set; }

        [Display(Name = "FilterCriteria2", ResourceType = typeof(Reports))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Criteria2 { get; set; }

        [DbIgnore]
        public List<string> CriteriaList { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string CriteriaValue
        {
            get
            {
                if (Column.FilterTypeId == (int)FilterTypes.Boolean)
                    return Criteria == "1" ? Reports.True : Reports.False;
                if (OperatorId == (int)FilterOperatorsAbstract.DateInterval)
                {
                    var key = ((FilterDateRanges)Criteria.ToInt()).ToString();
                    new ResourceDictionary("Filters").Dictionary.TryGetValue($"LabelDateRange_{key}", out var value);
                    return value.IsEmpty() ? key : value;
                }
                if (Column.FilterTypeId == (int)FilterTypes.Select)
                {
                    if (!IsMultipleSelect)
                        return FilterSelectListItems.FirstOrDefault(x => x.Value.Equals(Criteria, StringComparison.CurrentCultureIgnoreCase))?.Text;

                    var items = FilterSelectListItems.ToList().Where(x => !x.Value.IsEmpty()).GroupBy(x => x.Value).Select(x => x.First());
                    try
                    {
                        return ReportFilterCriteria.Select(x => {
                            var item = items.FirstOrDefault(y => y.Value.Equals(x.Value, StringComparison.CurrentCultureIgnoreCase));
                            return $"{item?.Text.Trim()} ({item?.Value.Trim()})".Trim();
                        }).OrderBy(x => x).Join().PrettyTrim(250);
                    }
                    catch { }
                }
                return Criteria.PrettyTrim(250);
            }
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DisplayOrder { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> FilterSelectListItems
        {
            get
            {
                if (!Column.IsSelect || Column.FilterQuery.IsEmpty())
                    return new List<SelectListItem>();
                return Report.Dataset.Database.Query<LookupItem>(Column.FilterQuery)
                    .Prepend(new LookupItem { Value = "", Text = Reports.FilterCriteria }, !IsMultipleSelect)
                    .ToSelectList(x => x.Text, x => x.Value);
            }
        }

        [DbIgnore]
        public bool IsLast { get; set; }

        [DbIgnore]
        public bool IsMultipleSelect => OperatorId == (int)FilterOperatorsAbstract.In || OperatorId == (int)FilterOperatorsAbstract.NotIn;

        [Display(Name = "FilterOperator", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int OperatorId { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> OperatorSelectListItems
        {
            get
            {
                Type operatorType;
                if (Report.Dataset.IsProc)
                    operatorType = typeof(FilterOperatorsBoolean);
                else
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
                return operatorType.TranslatedSelect(new ResourceDictionary("Filters"), "LabelFilter_");
            }
        }

        [DbIgnore, BindNever, ValidateNever]
        public string OperatorValue
        {
            get
            {
                var key = ((FilterOperatorsAbstract)OperatorId).ToString();
                new ResourceDictionary("Filters").Dictionary.TryGetValue($"LabelFilter_{key}", out var value);
                return value.IsEmpty() ? key : value;
            }
        }

        [BindNever, ValidateNever]
        public Report Report
        {
            get => _Report ?? (_Report = DbContext.Get<Report>(ReportId));
            set => _Report = value;
        }

        [BindNever, ValidateNever]
        public List<ReportFilterCriteria> ReportFilterCriteria
        {
            get => _ReportFilterCriteria ?? (_ReportFilterCriteria = DbContext.GetAll<ReportFilterCriteria>(new { ReportFilterId = Id }).ToList());
            set => _ReportFilterCriteria = value;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName => Report?.Name;

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

        public bool Save(bool lazySave = true)
        {
            DbContext.WithTransaction(() => {
                if (IsMultipleSelect)
                {
                    Criteria = null;
                    ReportFilterCriteria?.Where(criteria => CriteriaList?.Any(x => x.Equals(criteria.Value, StringComparison.CurrentCultureIgnoreCase)) != true).Each(x => {
                        DbContext.Delete(x);
                    });
                    CriteriaList?.Where(x => ReportFilterCriteria?.Any(y => string.Equals(y.Value, x, StringComparison.CurrentCultureIgnoreCase)) != true).Each(x => {
                        DbContext.Save(new ReportFilterCriteria(DbContext, Id, x, RequestUserId));
                    });
                }
                DbContext.Save(this);
            });
            return true;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (OperatorId == (int)FilterOperatorsAbstract.Range && Criteria2.IsEmpty())
                yield return new ValidationResult(Reports.ErrorRangeCriteria, new[] { "Criteria2" });
        }
    }
}
