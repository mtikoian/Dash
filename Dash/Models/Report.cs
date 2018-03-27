using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Jil;

namespace Dash.Models
{
    public enum Aggregators
    {
        Avg = 1, Count = 2, Max = 3, Min = 4, Sum = 5
    }

    [HasMany(typeof(ReportColumn))]
    [HasMany(typeof(ReportFilter))]
    [HasMany(typeof(ReportGroup))]
    [HasMany(typeof(ReportShare))]
    public class Report : BaseModel
    {
        private Dataset _Dataset;
        private List<ReportColumn> _ReportColumn;
        private List<ReportFilter> _ReportFilter;
        private List<ReportGroup> _ReportGroup;
        private List<ReportShare> _ReportShare;

        public Report()
        {
        }

        [JilDirective(true)]
        public int AggregatorId { get; set; }

        [Ignore, JilDirective(true)]
        public IEnumerable<object> AggregatorList
        {
            get
            {
                return typeof(Aggregators).TranslatedList(new ResourceDictionary("Charts"), "LabelAggregator_");
            }
        }

        [Ignore, JilDirective(true)]
        public bool AllowCloseParent { get; set; }

        [JilDirective(true)]
        public Dataset Dataset
        {
            get { return _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId)); }
            set { _Dataset = value; }
        }

        [Ignore, JilDirective(true)]
        public List<DatasetColumn> DatasetColumns { get { return _DatasetColumns ?? (_DatasetColumns = Dataset?.DatasetColumn ?? new List<DatasetColumn>()); } }

        [JilDirective(true)]
        public List<DatasetColumn> DatasetColumnsByDisplay
        {
            get
            {
                if (_DatasetColumnsByDisplay == null)
                {
                    _DatasetColumnsByDisplay = DatasetColumns.Where(x => !x.IsParam).ToList();
                    _DatasetColumnsByDisplay.Each(x => {
                        var col = ReportColumn.FirstOrDefault(c => c.ColumnId == x.Id);
                        if (col != null)
                        {
                            x.DisplayOrder = col.DisplayOrder;
                            x.ReportColumnId = col.Id;
                        }
                    });
                    _DatasetColumnsByDisplay = _DatasetColumnsByDisplay.OrderBy(r => r.DisplayOrder).ToList();
                }
                return _DatasetColumnsByDisplay;
            }
        }

        [Display(Name = "Dataset", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Ignore]
        public string DatasetName { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsOwner { get { return RequestUserId == OwnerId; } }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Ignore, JilDirective(true)]
        public User Owner { get { return _Owner ?? (_Owner = DbContext.Get<User>(OwnerId)); } }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int OwnerId { get; set; }

        [JilDirective(true)]
        public List<ReportColumn> ReportColumn
        {
            get { return _ReportColumn ?? (_ReportColumn = DbContext.GetAll<ReportColumn>(new { ReportId = Id }).ToList()); }
            set { _ReportColumn = value; }
        }

        [JilDirective(true)]
        public List<ReportFilter> ReportFilter
        {
            get { return _ReportFilter ?? (_ReportFilter = DbContext.GetAll<ReportFilter>(new { ReportId = Id }).ToList()); }
            set { _ReportFilter = value; }
        }

        [JilDirective(true)]
        public List<ReportGroup> ReportGroup
        {
            get { return _ReportGroup ?? (_ReportGroup = DbContext.GetAll<ReportGroup>(new { ReportId = Id }).ToList()); }
            set { _ReportGroup = value; }
        }

        [JilDirective(true)]
        public List<ReportShare> ReportShare
        {
            get { return _ReportShare ?? (_ReportShare = DbContext.GetAll<ReportShare>(new { ReportId = Id }).ToList()); }
            set { _ReportShare = value; }
        }

        [JilDirective(true)]
        public int RowLimit { get; set; } = 10;

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
                    shares = ReportShare
                }, JilOutputFormatter.Options);
            }
        }

        [JilDirective(true)]
        public decimal Width { get; set; } = 100;
        private List<DatasetColumn> _DatasetColumns { get; set; }
        private List<DatasetColumn> _DatasetColumnsByDisplay { get; set; }
        private User _Owner { get; set; }

        public Report Copy(string name = null)
        {
            var newReport = this.Clone();
            newReport.Id = 0;
            newReport.OwnerId = RequestUserId ?? 0;
            newReport.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;

            newReport.ReportColumn = (ReportColumn ?? DbContext.GetAll<ReportColumn>(new { ReportId = Id }))?.Select(x => new ReportColumn {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Width = x.Width,
                SortOrder = x.SortOrder,
                SortDirection = x.SortDirection
            }).ToList();

            newReport.ReportFilter = (ReportFilter ?? DbContext.GetAll<ReportFilter>(new { ReportId = Id }))?.Select(x => new ReportFilter {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Criteria = x.Criteria,
                Criteria2 = x.Criteria2,
                OperatorId = x.OperatorId
            }).ToList();

            newReport.ReportGroup = (ReportGroup ?? DbContext.GetAll<ReportGroup>(new { ReportId = Id }))?.Select(x => new ReportGroup {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder
            }).ToList();

            // don't copy the shares
            newReport.ReportShare = new List<ReportShare>();

            return newReport;
        }

        public void DataUpdate(int rowLimit, IEnumerable<TableSorting> sort = null)
        {
            if (!IsOwner)
            {
                return;
            }

            if (rowLimit != RowLimit)
            {
                RowLimit = rowLimit;
                DbContext.Save(this, false);
            }

            if (sort != null)
            {
                // building sorting objects from the querystring values
                var keyedSorts = sort.Where(x => x.Field != null).ToDictionary(x => x.Field.Replace("column", ""), x => x);
                var changed = false;
                ReportColumn.Each(x => {
                    changed = false;
                    if (keyedSorts.ContainsKey(x.ColumnId.ToString()))
                    {
                        var sortColumn = keyedSorts[x.ColumnId.ToString()];
                        var dir = sortColumn.Dir.ToUpper();

                        if (x.SortDirection != dir)
                        {
                            changed = true;
                            x.SortDirection = dir;
                        }
                        if (x.SortOrder != sortColumn.Index + 1)
                        {
                            changed = true;
                            x.SortOrder = sortColumn.Index + 1;
                        }
                    }
                    else
                    {
                        if (x.SortDirection != null)
                        {
                            changed = true;
                            x.SortDirection = null;
                        }
                        if (x.SortOrder != 0)
                        {
                            changed = true;
                            x.SortOrder = 0;
                        }
                    }

                    if (changed)
                    {
                        DbContext.Save(x);
                    }
                });
            }

            if (!ReportColumn.Any(c => c.SortDirection != null))
            {
                // make sure at least one column is sorted
                ReportColumn[0].SortDirection = "asc";
                ReportColumn[0].SortOrder = 1;
                DbContext.Save(ReportColumn[0]);
            }
        }

        public ReportResult GetData(IAppConfiguration appConfig, int start, int rowLimit, bool hasDatasetAccess)
        {
            // build a obj to store our results
            var response = new ReportResult() { UpdatedDate = DateUpdated };

            if (Dataset.DatasetColumn?.Any() != true)
            {
                // no reason to go any further if there are no columns for this dataset
                response.Error = Reports.ErrorInvalidReportId;
                return response;
            }

            var sqlQuery = new Query(this);
            if (!sqlQuery.HasColumns)
            {
                // if we didn't find any columns stop
                response.Error = Reports.ErrorNoColumnsSelected;
                return response;
            }

            var totalRecords = 0;
            var dataRes = new List<dynamic>();
            if (Dataset.IsProc)
            {
                dataRes = Dataset.Database.Query(sqlQuery.ExecStatement(), sqlQuery.Params).ToList();
                totalRecords = dataRes.Count();
            }
            else
            {
                // build the final sql for getting the record count
                var countSql = sqlQuery.CountStatement(true);
                if (hasDatasetAccess)
                {
                    response.CountSql = countSql;
                }

                // get the total record count
                try
                {
                    var countRes = Dataset.Database.Query(countSql, sqlQuery.Params);
                    if (countRes.Any())
                    {
                        totalRecords = ((IDictionary<string, object>)countRes.First())["cnt"].ToString().ToInt();
                    }
                }
                catch (Exception countEx)
                {
                    response.CountError = countEx.Message;
                }
            }

            rowLimit = rowLimit == 0 ? 10 : rowLimit;
            var totalPages = totalRecords > 0 ? Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(totalRecords) / Convert.ToDecimal(rowLimit))) : 0;
            if (start < 0)
            {
                start = 0;
            }
            var page = start / rowLimit;
            if (page > totalPages)
            {
                page = totalPages;
            }

            response.Page = page;
            response.Total = totalRecords;
            response.FilteredTotal = totalRecords;
            response.Rows = new List<object>();
            if (hasDatasetAccess)
            {
                response.DataSql = Dataset.IsProc ? sqlQuery.ExecStatement(true) : sqlQuery.SelectStatement(start, rowLimit, true);
            }

            try
            {
                if (!Dataset.IsProc)
                {
                    dataRes = Dataset.Database.Query(sqlQuery.SelectStatement(start, rowLimit), sqlQuery.Params).ToList();
                }
                if (dataRes.Any())
                {
                    response.Rows = ProcessData(dataRes, sqlQuery);
                }
            }
            catch (Exception dataEx)
            {
                response.DataError = dataEx.Message;
                response.Error = Reports.ErrorGettingData;
            }

            return response;
        }

        public object Lookups()
        {
            return Dataset?.GetSelectFilters(true).ToDictionary(x => x.Key, x => x.Value.Values);
        }

        public List<object> ProcessData(IEnumerable<dynamic> dataRes, Query sqlQuery)
        {
            var result = new List<object>();

            // get select filters for showing lookup data properly and figure out which we actually are using
            var replaceColumns = Dataset.GetSelectFilters().Where(x => sqlQuery.NeededColumns.ContainsKey(x.Key)).ToDictionary(x => "column" + x.Key, x => x.Value);
            var dateColumns = DatasetColumns.Where(col => col.IsDateTime).Select(c => c.Alias);
            var colummnMap = Dataset.IsProc ? DatasetColumns.ToDictionary(x => x.ColumnName, x => x.Alias) : null;

            // build the data result
            foreach (IDictionary<string, object> row in dataRes)
            {
                var dict = Dataset.IsProc ? row.ToDictionary(x => colummnMap[x.Key], x => x.Value) : row.ToDictionary(x => x.Key, x => x.Value);

                // replace any lookup values we can match
                replaceColumns.Where(x => dict.ContainsKey(x.Key) && dict[x.Key] != null).Each(x => {
                    var val = dict[x.Key].ToString();
                    if (x.Value.ContainsKey(val))
                    {
                        dict[x.Key] = x.Value[val].Text;
                    }
                });

                // date formatting
                dateColumns.Where(x => dict.ContainsKey(x) && dict[x] != null).Each(x => dict[x] = dict[x].ToDateTime());

                result.Add(dict);
            }

            return result;
        }

        public IEnumerable<object> ReportColumns()
        {
            if (ReportColumn == null)
            {
                return null;
            }
            return ReportColumn.Select(x => {
                var datasetColumn = Dataset.DatasetColumn.FirstOrDefault(c => c.Id == x.ColumnId);
                var link = datasetColumn?.Link;
                if (!link.IsEmpty())
                {
                    Dataset.DatasetColumn.ForEach(dc => link = link.ReplaceCase(dc.ColumnName, $"{{{dc.Alias}}}"));
                }
                return new {
                    Field = datasetColumn?.Alias ?? "",
                    Label = datasetColumn?.Title,
                    Sortable = true,
                    DataType = datasetColumn?.TableDataType ?? "",
                    Width = x.Width,
                    Links = new List<TableLink>().AddLink(link, Html.Classes().Append("target", "_blank"), render: !link.IsEmpty())
                };
            });
        }

        public IEnumerable<object> SortColumns()
        {
            if (ReportColumn == null)
            {
                return null;
            }
            var sortColumns = ReportColumn.Where(c => c.SortOrder > 0).OrderBy(c => c.SortOrder);
            return sortColumns.Select(x => {
                var datasetColumn = Dataset.DatasetColumn.FirstOrDefault(c => c.Id == x.ColumnId);
                return new { Field = datasetColumn?.Alias ?? "", Dir = x.SortDirection, DataType = datasetColumn?.TableDataType ?? "" };
            });
        }

        public void UpdateColumns(List<ReportColumn> newColumns)
        {
            var len = newColumns.Count();
            var currentFactor = Width / 100;
            for (var i = 0; i < len; i++)
            {
                // if this column was already being used, copy the properties we need from it
                var existingColumn = ReportColumn.Where(c => c.ColumnId == newColumns[i].ColumnId).FirstOrDefault();
                if (existingColumn != null)
                {
                    existingColumn.DisplayOrder = newColumns[i].DisplayOrder;
                    existingColumn.Width *= currentFactor;
                    newColumns[i] = existingColumn;
                }
                else
                {
                    newColumns[i].ReportId = Id;
                    newColumns[i].Width = 10;
                    Width += 10;
                }
            }

            // delete any removed columns
            ReportColumn.Where(c => !newColumns.Any(x => x.Id == c.Id)).ToList().ForEach(c => { DbContext.Delete(c); Width -= c.Width * currentFactor; });

            // gotta tweak the column widths to add up to 100%
            currentFactor = Width / 100;
            newColumns.ForEach(c => { c.Width = Math.Round(c.Width / currentFactor, 4); });

            // try saving
            DbContext.Save(this, false);
            newColumns.ForEach(r => DbContext.Save(r));
            ReportColumn = newColumns;
        }

        public void UpdateColumnWidths(decimal reportWidth, List<TableColumnWidth> newColumns = null)
        {
            if (Width != reportWidth)
            {
                Width = reportWidth;
                DbContext.Save(this, false);
            }

            var keyedReportColumns = ReportColumn.ToDictionary(x => x.ColumnId, x => x);
            newColumns.Each(x => {
                var columnId = x.Field.Replace("column", "").ToInt();
                if (keyedReportColumns.ContainsKey(columnId) && keyedReportColumns[columnId].Width != x.Width)
                {
                    keyedReportColumns[columnId].Width = x.Width;
                    DbContext.Save(keyedReportColumns[columnId]);
                }
            });
        }

        public List<ReportFilter> UpdateFilters(List<ReportFilter> newFilters = null)
        {
            newFilters?.Each(x => {
                if (x.CriteriaJson != null)
                {
                    x.Criteria = JSON.Serialize(x.CriteriaJson);
                }
                x.ReportId = Id;
                DbContext.Save(x);
            });
            var keyedFilters = newFilters?.ToDictionary(x => x.Id, x => x);
            if (ReportFilter?.Any() == true)
            {
                // delete any old filter that weren't in the new list
                ReportFilter.Where(x => keyedFilters?.ContainsKey(x.Id) != true).ToList().ForEach(x => DbContext.Delete(x));
            }
            return keyedFilters?.Values.ToList();
        }

        public List<ReportGroup> UpdateGroups(int groupAggregator, List<ReportGroup> newGroups = null)
        {
            if (AggregatorId != groupAggregator)
            {
                AggregatorId = groupAggregator;
                DbContext.Save(this, false);
            }

            // save the submitted groups
            var keyedGroups = new Dictionary<int, ReportGroup>();
            newGroups?.Each(x => {
                x.ReportId = Id;
                DbContext.Save(x);
                keyedGroups.Add(x.Id, x);
            });

            // delete any old groups that weren't in the new list
            ReportGroup?.Where(x => !keyedGroups.ContainsKey(x.Id)).ToList().ForEach(x => DbContext.Delete(x));

            return keyedGroups.Values.ToList();
        }
    }
}
