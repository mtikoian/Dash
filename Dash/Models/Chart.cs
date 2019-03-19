using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        HorizontalBar = 5,
        PolarArea = 6,
        Radar = 7
    }

    public enum DateIntervals
    {
        None = 11,
        OneMinute = 12,
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

    public class Chart : BaseModel
    {
        Regex _AlphaNumeric = new Regex("[^a-zA-Z0-9-]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
        List<ChartRange> _ChartRange;
        List<ChartShare> _ChartShare;

        public Chart() { }

        public static IEnumerable<SelectListItem> ChartTypeSelectList => typeof(ChartTypes).TranslatedSelect(new ResourceDictionary("Charts"), "LabelType_");

        [BindNever, ValidateNever]
        public List<ChartRange> ChartRange
        {
            get => _ChartRange ?? (_ChartRange = DbContext.GetAll<ChartRange>(new { ChartId = Id }).ToList());
            set => _ChartRange = value;
        }

        [BindNever, ValidateNever]
        public List<ChartShare> ChartShare
        {
            get => _ChartShare ?? (_ChartShare = DbContext.GetAll<ChartShare>(new { ChartId = Id }).ToList());
            set => _ChartShare = value;
        }

        [Display(Name = "Type", ResourceType = typeof(Charts))]
        public int ChartTypeId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsOwner
        {
            get
            {
                if (UserCreated == 0 && Id > 0)
                    UserCreated = DbContext.Get<Alert>(Id)?.UserCreated ?? 0;
                return RequestUserId == UserCreated;
            }
        }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsRadial => ChartTypeId == (int)ChartTypes.Pie || ChartTypeId == (int)ChartTypes.Doughnut || ChartTypeId == (int)ChartTypes.PolarArea;

        [Display(Name = "Name", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [DbIgnore]
        public int UserCreated { get; set; }

        public static Chart Create(CreateChart chart, int userId) => new Chart { Name = chart.Name, ChartTypeId = chart.ChartTypeId, RequestUserId = userId };

        public Chart Copy(string name = null)
        {
            var newChart = this.Clone();
            newChart.Id = 0;
            newChart.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;

            // duplicate the chart ranges
            newChart.ChartRange = (ChartRange ?? DbContext.GetAll<ChartRange>(new { ChartId = Id }))?.Select(x => new ChartRange {
                AggregatorId = x.AggregatorId,
                Color = x.Color,
                DateIntervalId = x.DateIntervalId,
                DisplayOrder = x.DisplayOrder,
                ReportId = x.ReportId,
                XAxisColumnId = x.XAxisColumnId,
                YAxisColumnId = x.YAxisColumnId
            }).ToList();

            // don't copy shares
            newChart.ChartShare = new List<ChartShare>();

            return newChart;
        }

        public ChartResult GetData(bool includeSql)
        {
            var response = new ChartResult() { UpdatedDate = DateUpdated, ChartId = Id, ChartName = Name, IsOwner = IsOwner };
            if (ChartRange?.Any() != true)
            {
                response.Error = Charts.ErrorNoRanges;
                return response;
            }

            response.Title = Name;
            response.ExportName = _AlphaNumeric.Replace(Name, "");
            response.Type = ChartTypeId > 0 ? ((ChartTypes)ChartTypeId).ToString().ToLower() : "";

            var chartResource = new ResourceDictionary("Charts");
            var reports = new Dictionary<int, Report>();

            foreach (var range in ChartRange)
            {
                if (!reports.ContainsKey(range.ReportId))
                {
                    reports.Add(range.ReportId, DbContext.Get<Report>(range.ReportId));
                }

                var report = reports[range.ReportId];
                if (report?.Dataset?.Database == null)
                {
                    continue;
                }

                var sqlQuery = new QueryBuilder(report, range);
                if (!(sqlQuery.HasColumns && sqlQuery.DatasetColumns.ContainsKey(range.XAxisColumnId) && sqlQuery.DatasetColumns.ContainsKey(range.YAxisColumnId)))
                {
                    continue;
                }
                sqlQuery.SelectStatement();

                var xColumn = sqlQuery.DatasetColumns[range.XAxisColumnId];
                var yColumn = sqlQuery.DatasetColumns[range.YAxisColumnId];
                var result = new ChartResultRange {
                    Color = range.Color,
                    CurrencyFormat = report.Dataset.CurrencyFormat,
                    DateFormat = report.Dataset.DateFormat,
                    Title = $"{report.Name} ({xColumn.Title})",
                    XType = xColumn.TableDataType.ToString(),
                    YType = yColumn.TableDataType.ToString(),
                    Sql = includeSql ? (report.Dataset.IsProc ? sqlQuery.ExecStatement() : sqlQuery.SqlResult.Sql) : null
                };

                if (range.AggregatorId == 0)
                {
                    range.AggregatorId = (int)Aggregators.Count;
                }

                result.YTitle = chartResource.ContainsKey("LabelAggregator_" + (Aggregators)range.AggregatorId) ? chartResource["LabelAggregator_" + (Aggregators)range.AggregatorId] : "";
                result.YTitle = $"{result.YTitle} {yColumn.Title}".Trim();

                // get the actual query data
                try
                {
                    var dataRes = report.Dataset.Database.Query(report.Dataset.IsProc ? sqlQuery.ExecStatement() : sqlQuery.SqlResult.Sql, report.Dataset.IsProc ? sqlQuery.Params : sqlQuery.SqlResult.NamedBindings);
                    if (dataRes.Any())
                    {
                        result.AddData(range, report.ProcessData(dataRes, sqlQuery), xColumn, yColumn);
                    }
                }
                catch (Exception dataEx)
                {
                    result.Error = dataEx.Message;
                }
                response.Ranges.Add(result);
            }

            if (response.Ranges.Any(x => !x.Error.IsEmpty()))
            {
                response.Error = Charts.ErrorGettingData;
            }
            else if (!response.Ranges.Any(x => x.Rows.Any()))
            {
                response.Error = Charts.ErrorNoData;
            }

            return response;
        }

        public bool Save(bool lazySave = true)
        {
            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                if (lazySave)
                {
                    DbContext.SaveMany(this, ChartRange);
                    DbContext.SaveMany(this, ChartShare);
                }
            });

            return true;
        }
    }
}
