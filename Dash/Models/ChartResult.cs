﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash.Models
{
    public class ChartResult : BaseModel
    {
        public int ChartId { get; set; }
        public string ChartName { get; set; }
        public string Error { get; set; }
        public string ExportName { get; set; }
        public bool IsOwner { get; set; }
        public List<ChartResultRange> Ranges { get; set; } = new List<ChartResultRange>();
        public string Title { get; set; }
        public string Type { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
    }

    public class ChartResultRange : BaseModel
    {
        public string Color { get; set; }
        public string CurrencyFormat { get; set; }
        public string DateFormat { get; set; }
        public string Error { get; set; }
        public List<string> Labels { get; set; } = new List<string>();
        public List<object> Rows { get; set; } = new List<object>();
        public string Sql { get; set; }
        public string Title { get; set; }
        public string XType { get; set; }
        public string YTitle { get; set; }
        public string YType { get; set; }

        public void AddData(ChartRange range, List<object> data, DatasetColumn xColumn, DatasetColumn yColumn)
        {
            var xName = xColumn.Alias;
            var yName = (Aggregators)range.AggregatorId != Aggregators.Count ? yColumn.Alias : "yValue";
            var asDictionary = data.Cast<IDictionary<string, object>>();

            if (!(range.DateIntervalId > 0 && (xColumn.IsDateTime || xColumn.IsTime)))
            {
                Labels = asDictionary.Select(x => x[xName].ToString()).ToList();
                Rows = asDictionary.Select(x => x[yName] ?? 0).ToList();
                return;
            }

            var yType = yColumn == null ? "string" : yColumn.TableDataType.ToString();
            var groupedValues = new Dictionary<string, dynamic>();
            var groupedCounts = new Dictionary<string, int>();

            if (range.FillDateGaps)
            {
                var dates = asDictionary.Select(x => x[xName].ToDateTime());
                var step = ((DateIntervals)range.DateIntervalId).ToIntervalStep();
                if (step.Ticks > 0)
                {
                    var stepDate = dates.Min();
                    var maxDate = dates.Max();
                    while (stepDate <= maxDate)
                    {
                        var container = stepDate.ToInterval((DateIntervals)range.DateIntervalId);
                        if (!groupedValues.ContainsKey(container))
                        {
                            groupedValues.Add(container, 0);
                            groupedCounts.Add(container, 0);
                        }
                        stepDate += step;
                    }
                }
            }

            asDictionary.Each(row => {
                var container = (row[xName] ?? "").ToDateTime().ToInterval((DateIntervals)range.DateIntervalId);
                if (groupedValues.ContainsKey(container))
                {
                    var value = groupedValues[container];
                    switch ((Aggregators)range.AggregatorId)
                    {
                        case Aggregators.Avg:
                            value = ((Convert.ToDecimal(value) * groupedCounts[container]) + Convert.ToDecimal(row[yName])) / (groupedCounts[container] + 1);
                            break;
                        case Aggregators.Max:
                            if (yType == "int" || yType == "currency")
                                value = (new[] { value, Convert.ToDecimal(row[yName]) }.Max());
                            else if (yType == "date")
                                value = (new[] { value, (row[yName] ?? "").ToDateTime() }.Max());
                            else
                                value = new[] { value, row[yName].ToString() }.Max();
                            break;
                        case Aggregators.Min:
                            if (yType == "int" || yType == "currency")
                                value = (new[] { value, Convert.ToDecimal(row[yName]) }.Min());
                            else if (yType == "date")
                                value = (new[] { value, (row[yName] ?? "").ToDateTime() }.Min());
                            else
                                value = new[] { value, row[yName].ToString() }.Min();
                            break;
                        case Aggregators.Count:
                        case Aggregators.Sum:
                            // sql query already aggregated count, so we need to sum those counts
                            value = Convert.ToDecimal(value) + Convert.ToDecimal(row[yName]);
                            break;
                    }
                    groupedCounts[container]++;
                    groupedValues[container] = value;
                }
                else
                {
                    groupedValues.Add(container, row[yName] ?? 0);
                    groupedCounts.Add(container, 1);
                }
            });

            Labels = groupedValues.Select(x => x.Key).ToList();
            Rows = groupedValues.Select(x => x.Value).ToList();
        }
    }
}
