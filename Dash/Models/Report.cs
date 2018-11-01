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
        private Dataset _Dataset;
        private List<DatasetColumn> _DatasetColumns;
        private List<DatasetColumn> _DatasetColumnsByDisplay;
        private List<ReportColumn> _ReportColumn;
        private List<ReportFilter> _ReportFilter;
        private List<ReportGroup> _ReportGroup;
        private List<ReportShare> _ReportShare;

        public Report()
        {
        }

        public int AggregatorId { get; set; }

        [BindNever, ValidateNever]
        public Dataset Dataset
        {
            get { return _Dataset ?? (_Dataset = DbContext.Get<Dataset>(DatasetId)); }
            set { _Dataset = value; }
        }

        [DbIgnore, BindNever, ValidateNever]
        public List<DatasetColumn> DatasetColumns { get { return _DatasetColumns ?? (_DatasetColumns = Dataset?.DatasetColumn ?? new List<DatasetColumn>()); } }

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

        [DbIgnore]
        public bool IsOwner { get { return RequestUserId == OwnerId; } }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int OwnerId { get; set; }

        [BindNever, ValidateNever]
        public List<ReportColumn> ReportColumn
        {
            get { return _ReportColumn ?? (_ReportColumn = DbContext.GetAll<ReportColumn>(new { ReportId = Id }).ToList()); }
            set { _ReportColumn = value; }
        }

        [BindNever, ValidateNever]
        public List<ReportFilter> ReportFilter
        {
            get { return _ReportFilter ?? (_ReportFilter = DbContext.GetAll<ReportFilter>(new { ReportId = Id }).ToList()); }
            set { _ReportFilter = value; }
        }

        [BindNever, ValidateNever]
        public List<ReportGroup> ReportGroup
        {
            get { return _ReportGroup ?? (_ReportGroup = DbContext.GetAll<ReportGroup>(new { ReportId = Id }).ToList()); }
            set { _ReportGroup = value; }
        }

        [BindNever, ValidateNever]
        public List<ReportShare> ReportShare
        {
            get { return _ReportShare ?? (_ReportShare = DbContext.GetAll<ReportShare>(new { ReportId = Id }).ToList()); }
            set { _ReportShare = value; }
        }

        public int RowLimit { get; set; } = 10;
        public decimal Width { get; set; } = 100;

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

        public ReportResult GetData(IAppConfiguration appConfig, int start, int rowLimit, bool includeSql)
        {
            // build a obj to store our results
            var response = new ReportResult() { UpdatedDate = DateUpdated, ReportId = Id, ReportName = Name, IsOwner = IsOwner };

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
                if (includeSql)
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
            if (includeSql)
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

        public bool Save(bool lazySave = true)
        {
            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                if (lazySave)
                {
                    DbContext.SaveMany(this, ReportColumn);
                    DbContext.SaveMany(this, ReportFilter);
                    DbContext.SaveMany(this, ReportGroup);
                    DbContext.SaveMany(this, ReportShare);
                }
            });

            return true;
        }

        public void UpdateColumns(List<ReportColumn> newColumns, int? userId = null)
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
            RequestUserId = userId ?? RequestUserId;
            DbContext.Save(this);
            newColumns.ForEach(x => {
                x.RequestUserId = userId ?? RequestUserId;
                DbContext.Save(x);
            });
            ReportColumn = newColumns;
        }

        public void UpdateColumnWidths(decimal reportWidth, List<TableColumnWidth> newColumns = null, int? userId = null)
        {
            if (Width != reportWidth)
            {
                Width = reportWidth;
                RequestUserId = userId ?? RequestUserId;
                DbContext.Save(this);
            }

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
