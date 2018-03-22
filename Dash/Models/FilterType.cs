using System.Collections.Generic;
using Dash.I18n;

namespace Dash.Models
{
    /// <summary>
    /// Define the operators available for date ranges.
    /// </summary>
    public enum FilterDateRanges
    {
        Today = 1,
        ThisWeek = 2,
        ThisMonth = 3,
        ThisQuarter = 4,
        ThisYear = 5,
        Yesterday = 6,
        LastWeek = 7,
        LastMonth = 8,
        LastQuarter = 9,
        LastYear = 10
    }

    /// <summary>
    /// Define all possible operators across all filter types.
    /// </summary>
    public enum FilterOperatorsAbstract
    {
        Equal = 1,
        NotEqual = 2,
        GreaterThan = 3,
        LessThan = 4,
        GreaterThanEqualTo = 5,
        LessThanEqualTo = 6,
        Range = 7,
        DateInterval = 8,
        In = 9,
        NotIn = 10,
        Like = 11,
        NotLike = 12
    }

    /// <summary>
    /// Define the operators available for boolean filter types only.
    /// </summary>
    public enum FilterOperatorsBoolean
    {
        Equal = 1
    }

    /// <summary>
    /// Define the operators available for date filter types only.
    /// </summary>
    public enum FilterOperatorsDate
    {
        Equal = 1,
        NotEqual = 2,
        GreaterThan = 3,
        LessThan = 4,
        GreaterThanEqualTo = 5,
        LessThanEqualTo = 6,
        Range = 7,
        DateInterval = 8
    }

    /// <summary>
    /// Define the operators available for numeric filter types only.
    /// </summary>
    public enum FilterOperatorsNumeric
    {
        Equal = 1,
        NotEqual = 2,
        GreaterThan = 3,
        LessThan = 4,
        GreaterThanEqualTo = 5,
        LessThanEqualTo = 6,
        Range = 7
    }

    /// <summary>
    /// Define the operators available for select filter types only.
    /// </summary>
    public enum FilterOperatorsSelect
    {
        Equal = 1,
        In = 9,
        NotIn = 10
    }

    /// <summary>
    /// Define the operators available for text filter types only.
    /// </summary>
    public enum FilterOperatorsText
    {
        Equal = 1,
        NotEqual = 2,
        In = 9,
        NotIn = 10,
        Like = 11,
        NotLike = 12
    }

    /// <summary>
    /// Define the possible types of filters.
    /// </summary>
    public enum FilterTypes
    {
        Boolean = 1,
        Date = 2,
        Select = 3,
        Numeric = 4,
        Text = 5,
        Binary = 6
    }

    /// <summary>
    /// FilterType is a type of filter criteria like bool, date, select, numeric, or text.
    /// </summary>
    public class FilterType
    {
        public static object DateOperators
        {
            get
            {
                return typeof(FilterDateRanges).TranslatedList(new ResourceDictionary("Filters"), "LabelDateRange_");
            }
        }

        public static Dictionary<int, object> FilterOperators
        {
            get
            {
                var filterResource = new ResourceDictionary("Filters");
                return new Dictionary<int, object>() {
                    { (int)FilterTypes.Boolean, typeof(FilterOperatorsBoolean).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) },
                    { (int)FilterTypes.Date, typeof(FilterOperatorsDate).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) },
                    { (int)FilterTypes.Numeric, typeof(FilterOperatorsNumeric).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) },
                    { (int)FilterTypes.Select, typeof(FilterOperatorsSelect).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) },
                    { (int)FilterTypes.Text, typeof(FilterOperatorsText).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) },
                    { (int)FilterTypes.Binary, typeof(FilterOperatorsText).TranslatedList(filterResource, "LabelFilter_").Prepend(new { Id = 0, Name = Reports.FilterOperator }) }
                };
            }
        }

        /// <summary>
        /// Build a list of all filter types.
        /// </summary>
        /// <returns>List of objects with id and name.</returns>
        public static IEnumerable<object> FilterTypeList
        {
            get
            {
                return typeof(FilterTypes).TranslatedList(new ResourceDictionary("Filters"), "LabelType_").Prepend(new { Id = 0, Name = Datasets.ColumnFilterType });
            }
        }
    }
}
