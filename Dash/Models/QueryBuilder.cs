using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata;
using SqlKata.Compilers;

namespace Dash.Models
{
    public class QueryBuilder
    {
        readonly bool _IsChart;
        ChartRange _ChartRange;
        Database _Database;
        bool _HasGrouping = false;
        Dictionary<string, DatasetJoin> _Joins;
        Dictionary<string, string> _NeededTables = new Dictionary<string, string>();
        Report _Report;
        Dictionary<int, ReportColumn> _ReportColumns;
        Dictionary<int, ReportGroup> _ReportGroups;

        static void CreateDateRange(FilterDateRanges range, out DateTime startDate, out DateTime endDate)
        {
            var today = DateTime.Today;
            startDate = today;
            endDate = today;

            switch (range)
            {
                case FilterDateRanges.Today:
                    endDate = today.AddDays(1).AddMilliseconds(-1);
                    break;
                case FilterDateRanges.ThisWeek:
                    startDate = today.StartOfWeek();
                    endDate = today.EndOfWeek();
                    break;
                case FilterDateRanges.ThisMonth:
                    startDate = today.StartOfMonth();
                    endDate = today.EndOfMonth();
                    break;
                case FilterDateRanges.ThisQuarter:
                    startDate = today.StartOfQuarter();
                    endDate = today.EndOfQuarter();
                    break;
                case FilterDateRanges.ThisYear:
                    startDate = today.StartOfYear();
                    endDate = today.EndOfYear();
                    break;
                case FilterDateRanges.Yesterday:
                    startDate = today.AddDays(-1);
                    endDate = today.AddMilliseconds(-1);
                    break;
                case FilterDateRanges.LastWeek:
                    startDate = today.AddDays(-7).StartOfWeek();
                    endDate = startDate.EndOfWeek();
                    break;
                case FilterDateRanges.LastMonth:
                    startDate = today.AddMonths(-1).StartOfMonth();
                    endDate = startDate.EndOfMonth();
                    break;
                case FilterDateRanges.LastQuarter:
                    startDate = today.AddMonths(-3).StartOfQuarter();
                    endDate = startDate.EndOfQuarter();
                    break;
                case FilterDateRanges.LastYear:
                    startDate = today.AddYears(-1).StartOfYear();
                    endDate = startDate.EndOfYear();
                    break;
                case FilterDateRanges.ThisHour:
                    startDate = DateTime.Now.StartOfHour();
                    endDate = startDate.EndOfHour();
                    break;
                case FilterDateRanges.ThisMinute:
                    startDate = DateTime.Now.StartOfMinute();
                    endDate = startDate.EndOfMinute();
                    break;
                case FilterDateRanges.LastMinute:
                    startDate = DateTime.Now.StartOfMinute().AddMinutes(-1);
                    endDate = startDate.EndOfMinute();
                    break;
            }
        }

        /// <summary>
        /// Build a list of joins necessary to include the requested table in a query.
        /// </summary>
        /// <param name="table">Table name to get joins for.</param>
        /// <returns>Returns updated dictionary of tables.</returns>
        Dictionary<string, string> AddJoin(Dictionary<string, string> tables, string table)
        {
            if (!_Joins.ContainsKey(table))
                return tables;

            var jTable = _Joins[table];
            if (tables.ContainsKey(jTable.TableName))
                return tables;

            tables.Add(jTable.TableName, jTable.Keys);
            // check if we need to add any tables to get the keys to join the current table
            foreach (var join in _Joins.Values)
                if (jTable.Keys.IndexOf(join.TableName + ".") > -1 && !tables.ContainsKey(join.TableName))
                    tables = AddJoin(tables, join.TableName);
            return tables;
        }

        /// <summary>
        /// Find the tables/joins needed to get this column.
        /// </summary>
        /// <param name="column">Column to find.</param>
        void AddNeededTables(DatasetColumn column)
        {
            if (!column.Derived.IsEmpty())
            {
                // check if any of the join tables are used in the derived sql
                foreach (var join in _Joins.Values)
                    if (column.Derived.IndexOf(join.TableName + ".") > -1 && !_NeededTables.ContainsKey(join.TableName))
                        // get the tables required to join this table
                        _NeededTables = AddJoin(_NeededTables, join.TableName);
            }
            else
            {
                // get the table name from the column name
                var table = column.TableName;
                if (!table.IsEmpty() && _Joins.ContainsKey(table))
                    // get the tables required to join this one in
                    _NeededTables = AddJoin(_NeededTables, table);
            }
        }

        /// <summary>
        /// Build the column list for the query.
        /// </summary>
        void BuildColumnSql()
        {
            var sql = "";
            if (_IsChart)
            {
                // if its a chart, we just need x and y columns
                // build the sql to get the x axis
                if (DatasetColumns.ContainsKey(_ChartRange.XAxisColumnId))
                {
                    sql = DatasetColumns[_ChartRange.XAxisColumnId].BuildSql(false);
                    if (sql.Length > 0)
                    {
                        sql += " AS " + DatasetColumns[_ChartRange.XAxisColumnId].Alias;

                        // add any tables this field may need
                        AddNeededTables(DatasetColumns[_ChartRange.XAxisColumnId]);
                        // add the column to the list
                        NeededColumns.Add(_ChartRange.XAxisColumnId, sql);
                        KataQuery.SelectRaw(sql);
                    }
                }

                // now the y axis
                _ChartRange.AggregatorId = _ChartRange.AggregatorId == 0 ? (int)Aggregators.Count : _ChartRange.AggregatorId;
                sql = ((Aggregators)_ChartRange.AggregatorId).ToString().ToUpper();

                if (DatasetColumns.ContainsKey(_ChartRange.YAxisColumnId) && ((Aggregators)_ChartRange.AggregatorId) != Aggregators.Count)
                {
                    sql += DatasetColumns[_ChartRange.YAxisColumnId].BuildSql();

                    // add tables this field will need
                    AddNeededTables(DatasetColumns[_ChartRange.YAxisColumnId]);

                    // add the column to the list
                    if (!NeededColumns.ContainsKey(_ChartRange.YAxisColumnId))
                    {
                        NeededColumns.Add(DatasetColumns[_ChartRange.YAxisColumnId].Id, sql);
                        KataQuery.SelectRaw(sql);
                    }
                }
                else
                {
                    // if no y axis, just use the primary key
                    sql += "(1)";
                    // add the column to the list
                    NeededColumns.Add(0, sql + " AS yValue");
                    KataQuery.SelectRaw(sql + " AS yValue");
                }
            }
            else
            {
                // build the columns for the query
                if (_HasGrouping && _Report.AggregatorId == 0)
                    _Report.AggregatorId = (int)Aggregators.Count;

                // iterate through all the columns in the dataset and see which ones we actually need
                _Report.Dataset.DatasetColumn.Where(x => !NeededColumns.ContainsKey(x.Id) && (_ReportColumns.ContainsKey(x.Id) || UsedInLink(x))).Each(column => {
                    // build the sql to get this column
                    sql = "";
                    if (_HasGrouping && !_ReportGroups.ContainsKey(column.Id))
                        sql = column.BuildSql(false, _Report.AggregatorId);
                    else if (_ReportGroups.ContainsKey(column.Id))
                        sql = column.BuildSql(false, (int)Aggregators.Max);
                    else
                        sql = column.BuildSql(false);

                    if (sql.Length > 0)
                    {
                        // add tables this field will need
                        AddNeededTables(column);

                        // add the column to the array of all needed columns
                        NeededColumns.Add(column.Id, sql + " AS " + column.Alias);
                        if (!_Database.AllowPaging)
                            NeededAliases.Add(column.Id, column.Alias);

                        KataQuery.SelectRaw(sql + " AS " + column.Alias);
                    }
                });
            }
        }

        /// <summary>
        /// Creates the SQL for grouping results.
        /// </summary>
        void BuildGroupBySql()
        {
            // add report grouping if any
            if (_Report.ReportGroup?.Any() == true)
            {
                _HasGrouping = true;
                _Report.ReportGroup.OrderBy(x => x.DisplayOrder).Each(x => {
                    KataQuery.GroupByRaw(DatasetColumns[x.ColumnId].BuildSql(false));
                    AddNeededTables(DatasetColumns[x.ColumnId]);
                });
            }
            // add chart grouping
            if (_IsChart && DatasetColumns.ContainsKey(_ChartRange.XAxisColumnId))
            {
                _HasGrouping = true;
                KataQuery.GroupByRaw(DatasetColumns[_ChartRange.XAxisColumnId].BuildSql(false));
                AddNeededTables(DatasetColumns[_ChartRange.XAxisColumnId]);
            }
        }

        /// <summary>
        /// Build the from/join sql statements.
        /// </summary>
        void BuildJoinSql()
        {
            var orderedJoins = new Dictionary<int, string>();
            foreach (var key in _NeededTables.Keys)
                if (key == _Report.Dataset.PrimarySource)
                    orderedJoins[0] = _Report.Dataset.PrimarySource;
                else
                    orderedJoins[_Joins[key].JoinOrder + 1] = key;

            // skip the primary source then start adding joins in the correct order
            orderedJoins.Skip(1).OrderBy(x => x.Key).Select(x => _Joins[x.Value]).Each(x => KataQuery.Join(x.TableName, j => j.WhereRaw(x.Keys), $"{((JoinTypes)x.JoinTypeId).ToString().ToUpper()} JOIN"));
        }

        /// <summary>
        /// Build the SQL for the order by statement.
        /// </summary>
        void BuildOrderBySql()
        {
            if (_IsChart)
            {
                KataQuery.OrderByRaw("1");
                return;
            }

            var sortStrings = _Report.ReportColumn.Where(c => c.SortOrder > 0).OrderBy(c => c.SortOrder).Where(x => DatasetColumns.ContainsKey(x.ColumnId)).ToList()
                .Select(x => {
                    var col = DatasetColumns[x.ColumnId];
                    var name = _Database.AllowPaging ? col.Alias : col.BuildSql(false, _HasGrouping ? _Report.AggregatorId : 0);
                    return $"{name} {x.SortDirection.ToUpper()}";
                });

            if (sortStrings.Any())
                KataQuery.OrderByRaw(sortStrings.Join());
            else
                KataQuery.OrderByRaw("1");
        }

        /// <summary>
        /// Build parameter list for proc calls.
        /// </summary>
        void BuildProcParams() => _Report.ReportFilter.Where(x => DatasetColumns.ContainsKey(x.ColumnId)).Each(x => {
            var column = DatasetColumns[x.ColumnId];
            if (column.IsParam)
                Params.Add(column.ColumnName, column.IsDateTime ? x.Criteria.ToDateTime().ToSqlDateTime() : x.Criteria);
        });

        /// <summary>
        /// Build the where statement for the sql query.
        /// </summary>
        void BuildWhereSql()
        {
            if (!_Report.Dataset.Conditions.IsEmpty())
                KataQuery.WhereRaw(_Report.Dataset.Conditions);

            // iterate through all the report filters we have
            if (_Report.ReportFilter != null && _Report.ReportFilter.Count > 0)
            {
                _Report.ReportFilter.Where(x => DatasetColumns.ContainsKey(x.ColumnId)).Each(x => {
                    var column = DatasetColumns[x.ColumnId];

                    if (column.IsParam)
                    {
                        Params.Add(column.ColumnName, column.IsDateTime ? x.Criteria.ToDateTime().ToSqlDateTime() : x.Criteria);
                    }
                    else
                    {
                        AddNeededTables(column);
                        var colAlias = column.BuildSql(false);
                        var value1 = column.IsDateTime ? x.Criteria.ToDateTime().ToSqlDateTime() : x.Criteria;
                        var value2 = x.Criteria2 != null ? (column.IsDateTime ? x.Criteria2.ToDateTime().ToSqlDateTime() : x.Criteria2) : null;

                        switch ((FilterOperatorsAbstract)x.OperatorId)
                        {
                            case FilterOperatorsAbstract.Equal:
                                KataQuery.WhereRaw($"{colAlias} = ?", value1);
                                break;
                            case FilterOperatorsAbstract.NotEqual:
                                KataQuery.WhereRaw($"{colAlias} != ?", value1);
                                break;
                            case FilterOperatorsAbstract.GreaterThan:
                                KataQuery.WhereRaw($"{colAlias} > ?", value1);
                                break;
                            case FilterOperatorsAbstract.LessThan:
                                KataQuery.WhereRaw($"{colAlias} < ?", value1);
                                break;
                            case FilterOperatorsAbstract.GreaterThanEqualTo:
                                KataQuery.WhereRaw($"{colAlias} >= ?", value1);
                                break;
                            case FilterOperatorsAbstract.LessThanEqualTo:
                                KataQuery.WhereRaw($"{colAlias} <= ?", value1);
                                break;
                            case FilterOperatorsAbstract.Range:
                                if (column.IsDateTime)
                                {
                                    var start = x.Criteria.ToDateTime();
                                    var end = x.Criteria2.ToDateTime();
                                    value1 = (start > end ? end : start).ToSqlDateTime();
                                    value2 = (start > end ? start : end).ToSqlDateTime();
                                }
                                else
                                {
                                    var start = x.Criteria.ToDouble();
                                    var end = x.Criteria2.ToDouble();
                                    value1 = Math.Min(start, end).ToString();
                                    value2 = Math.Max(start, end).ToString();
                                }
                                KataQuery.WhereRaw($"{colAlias} BETWEEN ? AND ?", value1, value2);
                                break;
                            case FilterOperatorsAbstract.In:
                                // kata doesn't like passing a single quoted list, tries to escape em unless we do it this way
                                // @todo should look at hardening this later to prevent injection
                                KataQuery.WhereRaw($"{colAlias} IN ({x.ReportFilterCriteria.Select(c => $"'{c.Value.Replace("'", "''")}'").Join()})");
                                break;
                            case FilterOperatorsAbstract.NotIn:
                                // kata doesn't like passing a single quoted list, tries to escape em unless we do it this way
                                KataQuery.WhereRaw($"{colAlias} NOT IN ({x.ReportFilterCriteria.Select(c => $"'{c.Value.Replace("'", "''")}'").Join()})");
                                break;
                            case FilterOperatorsAbstract.Like:
                                KataQuery.WhereRaw($"{colAlias} LIKE ?", $"%{value1}%");
                                break;
                            case FilterOperatorsAbstract.NotLike:
                                KataQuery.WhereRaw($"{colAlias} NOT LIKE ?", $"%{value1}%");
                                break;
                            case FilterOperatorsAbstract.DateInterval:
                                // handle special date functions
                                CreateDateRange((FilterDateRanges)x.Criteria.ToInt(), out var startDate, out var endDate);
                                KataQuery.WhereRaw($"{colAlias} BETWEEN ? AND ?", startDate.ToSqlDateTime(), endDate.ToSqlDateTime());
                                break;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Check if a dataset column is being used to build a link for a column that is displayed in the report.
        /// </summary>
        /// <param name="column">Dataset column to check usage for.</param>
        /// <returns>Returns true if any links need this column.</returns>
        bool UsedInLink(DatasetColumn column) => DatasetColumns.Values.Any(x => _ReportColumns.ContainsKey(x.Id) && !x.Link.IsEmpty() && x.Link.IndexOf(column.ColumnName, StringComparison.CurrentCultureIgnoreCase) > -1);

        /// <summary>
        /// Builds a SQL query for a report or chart range.
        /// </summary>
        /// <param name="report">Report object to build the query for.</param>
        /// <param name="range">Change range to build the query for.</param>
        public QueryBuilder(Report report, ChartRange range = null)
        {
            _Report = report;
            _ChartRange = range;
            _IsChart = range != null;
            _Database = _Report.Dataset.Database;

            _Joins = report.Dataset.DatasetJoin?.ToDictionary(j => j.TableName, j => j) ?? new Dictionary<string, DatasetJoin>();
            DatasetColumns = report.Dataset.DatasetColumn?.ToDictionary(j => j.Id, j => j) ?? new Dictionary<int, DatasetColumn>();
            _ReportColumns = _Report.ReportColumn?.Where(j => DatasetColumns.ContainsKey(j.ColumnId)).ToDictionary(j => j.ColumnId, j => j) ?? new Dictionary<int, ReportColumn>();
            _ReportGroups = _Report.ReportGroup?.Where(j => DatasetColumns.ContainsKey(j.ColumnId)).ToDictionary(j => j.ColumnId, j => j) ?? new Dictionary<int, ReportGroup>();

            // add the primaryTable to the list of needed tables. we always query it
            _NeededTables.Add(report.Dataset.PrimarySource, report.Dataset.PrimarySource);

            if (report.Dataset.IsProc)
            {
                BuildProcParams();
                return;
            }

            KataQuery.From(report.Dataset.PrimarySource);

            BuildGroupBySql();
            BuildColumnSql();
            BuildWhereSql();
            BuildOrderBySql();
            BuildJoinSql();
        }

        public Dictionary<int, DatasetColumn> DatasetColumns { get; set; }
        public bool HasColumns => NeededColumns.Count > 0 || _Report.Dataset.IsProc;
        public Query KataQuery { get; set; } = new Query();
        public Dictionary<int, string> NeededAliases { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> NeededColumns { get; set; } = new Dictionary<int, string>();
        public Dictionary<string, object> Params { get; } = new Dictionary<string, object>();
        public SqlResult SqlResult { get; set; }

        public void CountStatement()
        {
            if (_Report.Dataset.IsProc)
                return;

            // replace the selected columns with a count column, remove unneeded ordering
            var query = KataQuery.Clone();
            query.ClearComponent("select");
            query.ClearComponent("order");
            query.SelectRaw("COUNT(1) AS count");

            if (_Database.IsSqlServer)
                SqlResult = new SqlServerCompiler { UseLegacyPagination = !_Database.AllowPaging }.Compile(query);
            else
                SqlResult = new MySqlCompiler().Compile(query);
        }

        public string ExecStatement() => $"EXEC {_Report.Dataset.PrimarySource} " + Params.Select(x => $"@{x.Key} = @{x.Key}").Join();

        /// <summary>
        /// Build the complete SQL SELECT statement to get all columns. Optionally can be limited using start and rows.
        /// </summary>
        /// <param name="start">Row number to start at.</param>
        /// <param name="rows">Number of rows to return</param>
        public void SelectStatement(int start = 0, int rows = 0)
        {
            if (_Report.Dataset.IsProc)
                return;

            if (rows > 0)
            {
                KataQuery.Limit(rows);
                KataQuery.Offset(start);
            }

            if (_Database.IsSqlServer)
                SqlResult = new SqlServerCompiler { UseLegacyPagination = !_Database.AllowPaging }.Compile(KataQuery);
            else
                SqlResult = new MySqlCompiler().Compile(KataQuery);
        }
    }
}
