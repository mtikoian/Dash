using Dash.I18n;
using Jil;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dash.Models
{
    /// <summary>
    /// Chart is a chart derived from multiple reports.
    /// </summary>
    [HasMany(typeof(ChartRange))]
    [HasMany(typeof(ChartShare))]
    public class Chart : BaseModel
    {
        private Regex _AlphaNumeric = new Regex("[^a-zA-Z0-9-]", RegexOptions.Compiled);
        private List<ChartRange> _ChartRange;
        private List<ChartShare> _ChartShare;
        private User _Owner;
        private int CurrentUserId;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Chart(int userId)
        {
            CurrentUserId = userId;
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

        [Display(Name = "Type", ResourceType = typeof(I18n.Charts))]
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
        public bool IsOwner { get { return CurrentUserId == OwnerId; } }

        [Display(Name = "Name", ResourceType = typeof(I18n.Charts))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        /// <summary>
        /// Get the user object of the report owner.
        /// </summary>
        [Ignore, JilDirective(true)]
        public User Owner { get { return _Owner ?? (_Owner = DbContext.Get<User>(OwnerId)); } }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int OwnerId { get; set; }

        [Ignore, JilDirective(true)]
        public string ShareOptionsJson
        {
            get
            {
                return JSON.SerializeDynamic(new {
                    reportId = Id,
                    userList = DbContext.GetAll<User>(new { IsActive = 1 }).OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                        .Select(x => new { x.Id, x.FullName }).Prepend(new { Id = 0, FullName = Core.SelectUser }),
                    roleList = DbContext.GetAll<Role>().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name })
                        .Prepend(new { Id = 0, Name = Core.SelectRole }),
                    shares = ChartShare
                }, JilOutputFormatter.Options);
            }
        }

        /// <summary>
        /// Create a new chart object from a createChart object.
        /// </summary>
        /// <param name="chart">Setting for new chart.</param>
        /// <returns>Returns a new chart object.</returns>
        public static Chart Create(CreateChart chart, int userId)
        {
            return new Chart(userId) { Name = chart.Name, ChartTypeId = chart.ChartTypeId };
        }

        /// <summary>
        /// Copy a chart.
        /// </summary>
        /// <param name="name">New role name.</param>
        public Chart Copy(string name = null)
        {
            var newChart = this.Clone();
            newChart.Id = 0;
            newChart.OwnerId = CurrentUserId;
            newChart.Name = name.IsEmpty() ? String.Format(Core.CopyOf, Name) : name;

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

        /// <summary>
        /// Get the data for a chart.
        /// </summary>
        /// <param name="hasDatasetAccess">User has dataset access.</param>
        /// <returns>Returns the data for the chart as a dynamic object.</returns>
        public ChartResult GetData(bool hasDatasetAccess)
        {
            // build a obj to store our results
            var response = new ChartResult() { UpdatedDate = DateUpdated };

            // no reason to go any further if there are no ranges for this chart
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
                    XType = xColumn.TableDataType,
                    YType = yColumn.TableDataType,
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
                    IEnumerable<dynamic> dataRes = report.Dataset.Database.Query(AppConfig, report.Dataset.IsProc ? sqlQuery.ExecStatement() : sqlQuery.SelectStatement(), sqlQuery.Params);
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

        /// <summary>
        /// Update the chart ranges.
        /// </summary>
        /// <param name="newRanges">List of new chart ranges.</param>
        /// <returns>Returns the updated list of ranges.</returns>
        public List<ChartRange> UpdateRanges(List<ChartRange> newRanges = null)
        {
            // save the submitted ranges
            var keyedRanges = new Dictionary<int, ChartRange>();
            newRanges.ForEach(x => {
                x.ChartId = Id;
                DbContext.Save(x);
                keyedRanges.Add(x.Id, x);
            });

            // delete any old ranges that weren't in the new list
            if (ChartRange?.Any() == true)
            {
                ChartRange.Where(x => !keyedRanges.ContainsKey(x.Id)).ToList().ForEach(x => DbContext.Delete(x));
            }
            return keyedRanges.Values.ToList();
        }
    }
}