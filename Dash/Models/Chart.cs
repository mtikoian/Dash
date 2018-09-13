using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Dash.Resources;
using Dash.Utils;
using Jil;
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

    public class Chart : BaseModel
    {
        private Regex _AlphaNumeric = new Regex("[^a-zA-Z0-9-]", RegexOptions.Compiled);
        private List<ChartRange> _ChartRange;
        private List<ChartShare> _ChartShare;
        private User _Owner;

        public Chart()
        {
        }

        public static IEnumerable<SelectListItem> ChartTypeSelectList
        {
            get
            {
                return typeof(ChartTypes).TranslatedSelect(new ResourceDictionary("Charts"), "LabelType_");
            }
        }

        [Ignore, JilDirective(true)]
        public IEnumerable<object> AggregatorList
        {
            get
            {
                return typeof(Aggregators).TranslatedList(new ResourceDictionary("Charts"), "LabelAggregator_");
            }
        }

        [JilDirective(true)]
        public List<ChartRange> ChartRange
        {
            get { return _ChartRange ?? (_ChartRange = DbContext.GetAll<ChartRange>(new { ChartId = Id }).ToList()); }
            set { _ChartRange = value; }
        }

        public List<ChartShare> ChartShare
        {
            get { return _ChartShare ?? (_ChartShare = DbContext.GetAll<ChartShare>(new { ChartId = Id }).ToList()); }
            set { _ChartShare = value; }
        }

        [Display(Name = "Type", ResourceType = typeof(Charts))]
        [JilDirective(true)]
        public int ChartTypeId { get; set; }

        [Ignore, JilDirective(true)]
        public IEnumerable<object> ChartTypeList { get { return typeof(ChartTypes).TranslatedList(); } }

        [Ignore, JilDirective(true)]
        public IEnumerable<object> DateIntervalList
        {
            get
            {
                return typeof(DateIntervals).TranslatedList(new ResourceDictionary("Filters"), "LabelDateInterval_");
            }
        }

        [Ignore, JilDirective(true)]
        public bool IsOwner { get { return RequestUserId == OwnerId; } }

        [Display(Name = "Name", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Ignore, JilDirective(true)]
        public User Owner { get { return _Owner ?? (_Owner = DbContext.Get<User>(OwnerId)); } }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int OwnerId { get; set; }

        [Ignore, JilDirective(true)]
        public string ShareOptionsJson
        {
            get
            {
                return JSON.SerializeDynamic(new {
                    reportId = Id,
                    userList = DbContext.GetAll<User>().OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                        .Select(x => new { x.Id, x.FullName }).Prepend(new { Id = 0, FullName = Core.SelectUser }),
                    roleList = DbContext.GetAll<Role>().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name })
                        .Prepend(new { Id = 0, Name = Core.SelectRole }),
                    shares = ChartShare
                }, JilOutputFormatter.Options);
            }
        }

        public static Chart Create(CreateChart chart, int userId)
        {
            return new Chart { Name = chart.Name, ChartTypeId = chart.ChartTypeId, OwnerId = userId, RequestUserId = userId };
        }

        public Chart Copy(string name = null)
        {
            var newChart = this.Clone();
            newChart.Id = 0;
            newChart.OwnerId = RequestUserId ?? 0;
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

        public ChartResult GetData(bool hasDatasetAccess)
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

                var sqlQuery = new Query(report, range);
                if (!(sqlQuery.HasColumns && sqlQuery.DatasetColumns.ContainsKey(range.XAxisColumnId) && sqlQuery.DatasetColumns.ContainsKey(range.YAxisColumnId)))
                {
                    continue;
                }

                var xColumn = sqlQuery.DatasetColumns[range.XAxisColumnId];
                var yColumn = sqlQuery.DatasetColumns[range.YAxisColumnId];
                var result = new ChartResultRange {
                    Color = range.Color,
                    CurrencyFormat = report.Dataset.CurrencyFormat,
                    DateFormat = report.Dataset.DateFormat,
                    Title = $"{report.Name} ({xColumn.Title})",
                    XType = xColumn.TableDataType.ToString(),
                    YType = yColumn.TableDataType.ToString(),
                    Sql = hasDatasetAccess ? (report.Dataset.IsProc ? sqlQuery.ExecStatement(true) : sqlQuery.SelectStatement(prepare: true)) : null
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
                    var dataRes = report.Dataset.Database.Query(report.Dataset.IsProc ? sqlQuery.ExecStatement() : sqlQuery.SelectStatement(), sqlQuery.Params);
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
