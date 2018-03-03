namespace Dash.Models
{
    public enum ChartOperators
    {
        Sum,
        Avg,
        Count,
        Min,
        Max
    }

    public enum ChartTypes
    {
        Line = 1,
        Bar = 2,
        Pie = 3,
        Doughnut = 4,
        HorizontalBar = 5
    }

    public enum DateIntervals
    {
        None = 11,
        FiveMinutes = 10,
        TenMinutes = 9,
        FifteenMinutes = 8,
        ThirtyMinutes = 7,
        Hour = 6,
        Day = 5,
        Week = 4,
        Month = 3,
        Quarter = 2,
        Year = 1
    }

    /// <summary>
    /// ChartType is a single type of chart.
    /// </summary>
    public class ChartType
    {
    }
}