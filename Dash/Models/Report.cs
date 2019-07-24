using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public enum Aggregators
    {
        Avg = 1, Count = 2, Max = 3, Min = 4, Sum = 5
    }

    public class Report : BaseModel
    {
        Dataset _Dataset;
        List<DatasetColumn> _DatasetColumns;
        List<DatasetColumn> _DatasetColumnsByDisplay;
        List<ReportColumn> _ReportColumn;
        List<ReportFilter> _ReportFilter;
        List<ReportGroup> _ReportGroup;
        List<ReportShare> _ReportShare;

        public Report() { }

        public int AggregatorId { get; set; }

        [BindNever, ValidateNever]
        public Dataset Dataset
        {
            get => _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId));
            set => _Dataset = value;
        }

        [DbIgnore, BindNever, ValidateNever]
        public List<DatasetColumn> DatasetColumns => _DatasetColumns ?? (_DatasetColumns = Dataset?.DatasetColumn ?? new List<DatasetColumn>());

        [BindNever, ValidateNever]
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

        [DbIgnore]
        public string DatasetName { get; set; }

        [DbIgnore]
        public bool IsDashboard { get; set; } = false;

        [DbIgnore, BindNever, ValidateNever]
        public bool IsOwner
        {
            get
            {
                if (UserCreated == 0 && Id > 0)
                    UserCreated = DbContext.Get<Report>(Id)?.UserCreated ?? 0;
                return RequestUserId == UserCreated;
            }
        }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [BindNever, ValidateNever]
        public List<ReportColumn> ReportColumn
        {
            get => _ReportColumn ?? (_ReportColumn = DbContext.GetAll<ReportColumn>(new { ReportId = Id }).ToList());
            set => _ReportColumn = value;
        }

        [BindNever, ValidateNever]
        public List<ReportFilter> ReportFilter
        {
            get => _ReportFilter ?? (_ReportFilter = DbContext.GetAll<ReportFilter>(new { ReportId = Id }).ToList());
            set => _ReportFilter = value;
        }

        [BindNever, ValidateNever]
        public List<ReportGroup> ReportGroup
        {
            get => _ReportGroup ?? (_ReportGroup = DbContext.GetAll<ReportGroup>(new { ReportId = Id }).ToList());
            set => _ReportGroup = value;
        }

        [BindNever, ValidateNever]
        public List<ReportShare> ReportShare
        {
            get => _ReportShare ?? (_ReportShare = DbContext.GetAll<ReportShare>(new { ReportId = Id }).ToList());
            set => _ReportShare = value;
        }

        public int RowLimit { get; set; } = 10;

        [DbIgnore]
        public int UserCreated { get; set; }

        public Report Copy(string name = null)
        {
            var newReport = this.Clone();
            newReport.Id = 0;
            newReport.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;

            newReport.ReportColumn = ReportColumn?.Select(x => new ReportColumn {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Width = x.Width,
                SortOrder = x.SortOrder,
                SortDirection = x.SortDirection
            }).ToList();

            newReport.ReportGroup = ReportGroup?.Select(x => new ReportGroup {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder
            }).ToList();

            newReport.ReportFilter = ReportFilter?.Select(x => new ReportFilter {
                ColumnId = x.ColumnId,
                DisplayOrder = x.DisplayOrder,
                Criteria = x.Criteria,
                Criteria2 = x.Criteria2,
                OperatorId = x.OperatorId
            }).ToList();

            // don't copy the shares
            newReport.ReportShare = new List<ReportShare>();

            return newReport;
        }

        public void DataUpdate(int rowLimit, IEnumerable<TableSorting> sort = null)
        {
            if (!IsOwner)
                return;

            if (rowLimit != RowLimit)
            {
                RowLimit = rowLimit;
                DbContext.Save(this);
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
                        var dir = sortColumn.SortDir.ToUpper();

                        if (x.SortDirection != dir)
                        {
                            changed = true;
                            x.SortDirection = dir;
                        }
                        if (x.SortOrder != sortColumn.SortOrder + 1)
                        {
                            changed = true;
                            x.SortOrder = sortColumn.SortOrder + 1;
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
                        DbContext.Save(x);
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

        public ReportResult GetData(IAppConfiguration appConfig, int start, int rowLimit, bool includeSql)
        {
            // build a obj to store our results
            var response = new ReportResult { UpdatedDate = DateUpdated, ReportId = Id, ReportName = Name, IsOwner = IsOwner };

            if (Dataset.DatasetColumn?.Any() != true)
            {
                // no reason to go any further if there are no columns for this dataset
                response.Error = Reports.ErrorInvalidReportId;
                return response;
            }

            var sqlQuery = new QueryBuilder(this);
            if (!sqlQuery.HasColumns)
            {
                // if we didn't find any columns stop
                response.Error = Reports.ErrorNoColumnsSelected;
                return response;
            }

            long totalRecords = 0;
            var dataRes = new List<dynamic>();
            if (Dataset.IsProc)
            {
                try
                {
                    dataRes = Dataset.Database.Query(sqlQuery.ExecStatement(), sqlQuery.Params).ToList();
                    totalRecords = dataRes.Count();
                }
                catch (Exception execEx)
                {
                    response.DataError = execEx.Message;
                    response.Error = Reports.ErrorGettingData;
                }
            }
            else
            {
                // build the final sql for getting the record count
                sqlQuery.CountStatement();
                if (includeSql)
                    response.CountSql = sqlQuery.SqlResult.Sql;

                // get the total record count
                try
                {
                    // when using a group by should use number of rows, not count
                    var countRes = Dataset.Database.Query(sqlQuery.SqlResult.Sql, sqlQuery.SqlResult.NamedBindings);
                    if (countRes.Any())
                        totalRecords = countRes.LongCount() > 1 ? countRes.LongCount() : ((IDictionary<string, object>)countRes.First())["count"].ToString().ToLong();
                }
                catch (Exception countEx)
                {
                    response.CountError = countEx.Message;
                }
            }

            rowLimit = rowLimit == 0 ? 10 : rowLimit;
            start = Math.Max(start, 0);

            response.Page = Math.Min(start / rowLimit, totalRecords > 0 ? Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(totalRecords) / Convert.ToDecimal(rowLimit))) : 0);
            response.Total = totalRecords;
            response.FilteredTotal = totalRecords;
            response.Rows = new List<object>();

            sqlQuery.SelectStatement(start, rowLimit);
            if (includeSql)
                response.DataSql = Dataset.IsProc ? sqlQuery.ExecStatement() : sqlQuery.SqlResult.Sql;

            try
            {
                if (!Dataset.IsProc)
                    dataRes = Dataset.Database.Query(sqlQuery.SqlResult.Sql, sqlQuery.SqlResult.NamedBindings).ToList();
                if (dataRes.Any())
                    response.Rows = ProcessData(dataRes, sqlQuery);
            }
            catch (Exception dataEx)
            {
                response.DataError = dataEx.Message;
                response.Error = Reports.ErrorGettingData;
            }

            return response;
        }

        public List<object> ProcessData(IEnumerable<dynamic> dataRes, QueryBuilder sqlQuery)
        {
            var result = new List<object>();

            // get select filters for showing lookup data properly and figure out which we actually are using
            var replaceColumns = Dataset.GetSelectFilters().Where(x => sqlQuery.NeededColumns.ContainsKey(x.Key)).ToDictionary(x => "column" + x.Key, x => x.Value);
            var dateColumns = DatasetColumns.Where(col => col.IsDateTime).Select(c => c.Alias);
            var timeColumns = DatasetColumns.Where(col => col.IsTime).Select(c => c.Alias);
            var columnMap = Dataset.IsProc ? DatasetColumns.ToDictionary(x => x.ColumnName, x => x.Alias) : null;

            // build the data result
            foreach (IDictionary<string, object> row in dataRes)
            {
                var dict = Dataset.IsProc ? row.ToDictionary(x => columnMap[x.Key], x => x.Value) : row.ToDictionary(x => x.Key, x => x.Value);

                // replace any lookup values we can match
                replaceColumns.Where(x => dict.ContainsKey(x.Key) && dict[x.Key] != null).Each(x => {
                    var val = dict[x.Key].ToString();
                    if (x.Value.ContainsKey(val))
                        dict[x.Key] = x.Value[val].Text;
                });

                // date formatting
                dateColumns.Where(x => dict.ContainsKey(x) && dict[x] != null).Each(x => dict[x] = dict[x].ToDateTime());

                // time formatting
                timeColumns.Where(x => dict.ContainsKey(x) && dict[x] != null).Each(x => dict[x] = dict[x].ToTimespan().ToString());

                result.Add(dict);
            }

            return result;
        }

        public bool Save(bool lazySave = true)
        {
            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                if (lazySave)
                {
                    DbContext.SaveMany(this, ReportColumn);
                    DbContext.SaveMany(this, ReportFilter);
                    DbContext.SaveMany(this, ReportShare);
                    DbContext.SaveMany(this, ReportGroup);
                }
            });

            return true;
        }

        public void UpdateColumns(List<ReportColumn> newColumns, int? userId = null)
        {
            var len = newColumns.Count();
            for (var i = 0; i < len; i++)
            {
                // if this column was already being used, copy the properties we need from it
                var existingColumn = ReportColumn.Where(c => c.ColumnId == newColumns[i].ColumnId).FirstOrDefault();
                if (existingColumn != null)
                {
                    existingColumn.DisplayOrder = newColumns[i].DisplayOrder;
                    newColumns[i] = existingColumn;
                }
                else
                {
                    newColumns[i].ReportId = Id;
                }
            }

            // delete any removed columns
            ReportColumn.Where(c => !newColumns.Any(x => x.Id == c.Id)).ToList().ForEach(c => {
                DbContext.Delete(c);
            });

            // try saving
            RequestUserId = userId ?? RequestUserId;
            DbContext.Save(this);
            newColumns.ForEach(x => {
                x.RequestUserId = userId ?? RequestUserId;
                DbContext.Save(x);
            });
            ReportColumn = newColumns;
        }

        public void UpdateColumnWidths(List<TableColumnWidth> newColumns = null, int? userId = null)
        {
            var keyedReportColumns = ReportColumn.ToDictionary(x => x.ColumnId, x => x);
            newColumns.Each(x => {
                var columnId = x.Field.Replace("column", "").ToInt();
                if (keyedReportColumns.ContainsKey(columnId) && keyedReportColumns[columnId].Width != x.Width)
                {
                    keyedReportColumns[columnId].Width = x.Width;
                    keyedReportColumns[columnId].RequestUserId = userId ?? RequestUserId;
                    DbContext.Save(keyedReportColumns[columnId]);
                }
            });
        }
    }
}
