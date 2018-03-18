using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Jil;

namespace Dash.Models
{
    /// <summary>
    /// List the allowed group aggregators.
    /// </summary>
    public enum Aggregators
    {
        Avg = 1, Count = 2, Max = 3, Min = 4, Sum = 5
    }

    /// <summary>
    /// Report is a report derived from one dataset.
    /// </summary>
    [HasMany(typeof(ReportColumn))]
    [HasMany(typeof(ReportFilter))]
    [HasMany(typeof(ReportGroup))]
    [HasMany(typeof(ReportShare))]
    public class Report : BaseModel
    {
        private int _CurrentUserId;
        private Dataset _Dataset;
        private List<ReportColumn> _ReportColumn;
        private List<ReportFilter> _ReportFilter;
        private List<ReportGroup> _ReportGroup;
        private List<ReportShare> _ReportShare;

        public Report()
        {
        }

        public Report(int userId)
        {
            _CurrentUserId = userId;
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
        public bool CloseParent { get; set; }

        [JilDirective(true)]
        public Dataset Dataset
        {
            get { return _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId)); }
            set { _Dataset = value; }
        }

        /// <summary>
        /// This is used in views, but never saved.
        /// </summary>
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

        [Display(Name = "Dataset", ResourceType = typeof(I18n.Reports))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Ignore]
        public string DatasetName { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsOwner { get { return _CurrentUserId == OwnerId; } }

        [Display(Name = "Name", ResourceType = typeof(I18n.Reports))]
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

        /// <summary>
        /// Copy a report.
        /// </summary>
        /// <param name="name">New role name.</param>
        public Report Copy(string name = null)
        {
            var newReport = this.Clone();
            newReport.Id = 0;
            newReport.OwnerId = _CurrentUserId;
            newReport.Name = name.IsEmpty() ? String.Format(Core.CopyOf, Name) : name;

            // duplicate the report columns
            newReport.ReportColumn = (ReportColumn ?? DbContext.GetAll<ReportColumn>(new { ReportId = Id }))?.Select(x => new ReportColumn {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Width = x.Width,
                SortOrder = x.SortOrder,
                SortDirection = x.SortDirection
            }).ToList();

            // duplicate the report filters
            newReport.ReportFilter = (ReportFilter ?? DbContext.GetAll<ReportFilter>(new { ReportId = Id }))?.Select(x => new ReportFilter {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Criteria = x.Criteria,
                Criteria2 = x.Criteria2,
                OperatorId = x.OperatorId
            }).ToList();

            // duplicate the report groups
            newReport.ReportGroup = (ReportGroup ?? DbContext.GetAll<ReportGroup>(new { ReportId = Id }))?.Select(x => new ReportGroup {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder
            }).ToList();

            // don't copy the shares
            newReport.ReportShare = new List<ReportShare>();

            return newReport;
        }

        /// <summary>
        /// Save changes to the report coming from the data ajax request.
        /// </summary>
        /// <param name="rows">Number of rows to return.</param>
        /// <param name="sort">Sorting criteria</param>
        public void DataUpdate(int rows, IEnumerable<TableSorting> sort = null)
        {
            if (!IsOwner)
            {
                return;
            }

            // check for row limit change
            if (rows != RowLimit)
            {
                RowLimit = rows;
                DbContext.Save(this, false);
            }

            // get any sorting columns
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

        /// <summary>
        /// Get the data for the report.
        /// </summary>
        /// <param name="start">Row number to start at.</param>
        /// <param name="rows">Number of rows to return.</param>
        /// <param name="hasDatasetAccess">User has access to dataset.</param>
        /// <returns>Returns the data for the report.</returns>
        public ReportResult GetData(IAppConfiguration appConfig, int start, int rows, bool hasDatasetAccess)
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
            IEnumerable<dynamic> dataRes = new List<dynamic>();
            if (Dataset.IsProc)
            {
                dataRes = Dataset.Database.Query(appConfig, sqlQuery.ExecStatement(), sqlQuery.Params);
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
                    IEnumerable<dynamic> countRes = Dataset.Database.Query(appConfig, countSql, sqlQuery.Params);
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

            // figure out the row limit
            rows = rows == 0 ? 25 : rows;
            // calculate the total number of pages
            int totalPages = totalRecords > 0 ? Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(totalRecords) / Convert.ToDecimal(rows))) : 0;
            if (start < 0)
            {
                start = 0;
            }
            // max sure we aren't past the end
            int page = start / rows;
            if (page > totalPages)
            {
                page = totalPages;
            }

            // add the page number, total records, etc to our response object
            response.Page = page;
            response.Total = totalRecords;
            response.FilteredTotal = totalRecords;
            response.Rows = new List<object>();
            if (hasDatasetAccess)
            {
                response.DataSql = Dataset.IsProc ? sqlQuery.ExecStatement(true) : sqlQuery.SelectStatement(start, rows, true);
            }

            // get the actual query data
            try
            {
                if (!Dataset.IsProc)
                {
                    dataRes = Dataset.Database.Query(appConfig, sqlQuery.SelectStatement(start, rows), sqlQuery.Params);
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

        /// <summary>
        /// Get the column filter lookups for the report.
        /// </summary>
        /// <returns>List of column ids and the lookup values.</returns>
        public object Lookups()
        {
            return Dataset?.GetSelectFilters(true).ToDictionary(x => x.Key, x => x.Value.Values);
        }

        /// <summary>
        /// Processes the data from the query replacing lookup values, formatting data, and creating an arraylist of rows.
        /// </summary>
        /// <param name="dataRes">Input data as enumerable of dynamic objects.</param>
        /// <param name="sqlQuery">SqlQuery object with all the info for the query that produced this data.</param>
        /// <returns>Returns an arraylist of dictionary objects.</returns>
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

        /// <summary>
        /// Get a list of columns used in the report, for use displaying the report.
        /// </summary>
        /// <returns>List of objects with field, label, sortable, datatype, width, and links.</returns>
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

        /// <summary>
        /// Get the columns used to sort the report, for use displaying the report.
        /// </summary>
        /// <returns>List of objects with field, dir, and datatype.</returns>
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

        /// <summary>
        /// Update the widths of columns after adding/removing columns.
        /// </summary>
        /// <param name="newColumns">List of new report columns</param>
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

        /// <summary>
        /// Update the report column widths, without adding/removing columns.
        /// </summary>
        /// <param name="reportWidth">Total width of report as %.</param>
        /// <param name="newColumns">List of new report columns.</param>
        /// <returns>Returns the updated list of groups.</returns>
        public void UpdateColumnWidths(decimal reportWidth, List<TableColumnWidth> newColumns = null)
        {
            if (Width != reportWidth)
            {
                Width = reportWidth;
                DbContext.Save(this, false);
            }

            // make a list of all report columns keyed on column id
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

        /// <summary>
        /// Update the report filters.
        /// </summary>
        /// <param name="newFilters">List of new report filters.</param>
        /// <returns>Returns the updated list of filters.</returns>
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

        /// <summary>
        /// Update the report groups.
        /// </summary>
        /// <param name="groupAggregator">Group aggregator ID</param>
        /// <param name="newGroups">List of new report groups.</param>
        /// <returns>Returns the updated list of groups.</returns>
        public List<ReportGroup> UpdateGroups(int groupAggregator, List<ReportGroup> newGroups = null)
        {
            if (AggregatorId != groupAggregator)
            {
                AggregatorId = groupAggregator;
                DbContext.Save(this, false);
            }

            // save the submitted groups
            var keyedGroups = new Dictionary<int, ReportGroup>();
            if (newGroups != null)
            {
                foreach (var group in newGroups)
                {
                    group.ReportId = Id;
                    DbContext.Save(group);
                    keyedGroups.Add(group.Id, group);
                }
            }

            // delete any old groups that weren't in the new list
            if (ReportGroup != null && ReportGroup.Any())
            {
                ReportGroup.Where(x => !keyedGroups.ContainsKey(x.Id)).ToList().ForEach(x => DbContext.Delete(x));
            }
            return keyedGroups.Values.ToList();
        }
    }
}