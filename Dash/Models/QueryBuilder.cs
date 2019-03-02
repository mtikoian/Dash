using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata;
using SqlKata.Compilers;

namespace Dash.Models
{
    public class QueryBuilder
    {
        private bool HasGrouping = false;
        private bool IsChart;
        private ChartRange ChartRange;
        private Database Database;
        private DatabaseTypes DbType;
        private Dictionary<int, ReportColumn> ReportColumns;
        private Dictionary<string, DatasetJoin> Joins;
        private Dictionary<string, string> NeededTables = new Dictionary<string, string>();
        private Report Report;

        /// <summary>
        /// Build a list of joins necessary to include the requested table in a query.
        /// </summary>
        /// <param name="table">Table name to get joins for.</param>
        /// <returns>Returns updated dictionary of tables.</returns>
        private Dictionary<string, string> AddJoin(Dictionary<string, string> tables, string table)
        {
            if (!Joins.ContainsKey(table))
            {
                return tables;
            }
            var jTable = Joins[table];
            if (tables.ContainsKey(jTable.TableName))
            {
                return tables;
            }

            tables.Add(jTable.TableName, jTable.Keys);
            // check if we need to add any tables to get the keys to join the current table
            foreach (var join in Joins.Values)
            {
                if (jTable.Keys.IndexOf(join.TableName + ".") > -1 && !tables.ContainsKey(join.TableName))
                {
                    tables = AddJoin(tables, join.TableName);
                }
            }
            return tables;
        }

        /// <summary>
        /// Find the tables/joins needed to get this column.
        /// </summary>
        /// <param name="column">Column to find.</param>
        private void AddNeededTables(DatasetColumn column)
        {
            if (!column.Derived.IsEmpty())
            {
                // check if any of the join tables are used in the derived sql
                foreach (var join in Joins.Values)
                {
                    if (column.Derived.IndexOf(join.TableName + ".") > -1 && !NeededTables.ContainsKey(join.TableName))
                    {
                        // get the tables required to join this table
                        NeededTables = AddJoin(NeededTables, join.TableName);
                    }
                }
            }
            else
            {
                // get the table name from the column name
                var table = column.TableName;
                if (!table.IsEmpty() && Joins.ContainsKey(table))
                {
                    // get the tables required to join this one in
                    NeededTables = AddJoin(NeededTables, table);
                }
            }
        }

        /// <summary>
        /// Build the column list for the query.
        /// </summary>
        private void BuildColumnSql()
        {
            var sql = "";
            if (IsChart)
            {
                // if its a chart, we just need x and y columns
                // build the sql to get the x axis
                if (DatasetColumns.ContainsKey(ChartRange.XAxisColumnId))
                {
                    sql = DatasetColumns[ChartRange.XAxisColumnId].BuildSql(false);
                    if (sql.Length > 0)
                    {
                        sql += " AS " + DatasetColumns[ChartRange.XAxisColumnId].Alias;

                        // add any tables this field may need
                        AddNeededTables(DatasetColumns[ChartRange.XAxisColumnId]);
                        // add the column to the list
                        NeededColumns.Add(ChartRange.XAxisColumnId, sql);
                        KataQuery.SelectRaw(sql);
                    }
                }

                // now the y axis
                ChartRange.AggregatorId = ChartRange.AggregatorId == 0 ? (int)Aggregators.Count : ChartRange.AggregatorId;
                sql = ((Aggregators)ChartRange.AggregatorId).ToString().ToUpper();

                if (DatasetColumns.ContainsKey(ChartRange.YAxisColumnId) && ((Aggregators)ChartRange.AggregatorId) != Aggregators.Count)
                {
                    sql += DatasetColumns[ChartRange.YAxisColumnId].BuildSql();

                    // add tables this field will need
                    AddNeededTables(DatasetColumns[ChartRange.YAxisColumnId]);

                    // add the column to the list
                    if (!NeededColumns.ContainsKey(ChartRange.YAxisColumnId))
                    {
                        NeededColumns.Add(DatasetColumns[ChartRange.YAxisColumnId].Id, sql);
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
                if (HasGrouping && Report.AggregatorId == 0)
                {
                    Report.AggregatorId = (int)Aggregators.Count;
                }

                // iterate through all the columns in the dataset and see which ones we actually need
                foreach (var column in Report.Dataset.DatasetColumn)
                {
                    if (!NeededColumns.ContainsKey(column.Id) && (ReportColumns.ContainsKey(column.Id) || UsedInLink(column)))
                    {
                        // build the sql to get this column
                        sql = HasGrouping ? column.BuildSql(false, Report.AggregatorId) : column.BuildSql(false);
                        if (sql.Length > 0)
                        {
                            // add tables this field will need
                            AddNeededTables(column);

                            // add the column to the array of all needed columns
                            NeededColumns.Add(column.Id, sql + " AS " + column.Alias);
                            if (!Database.AllowPaging)
                            {
                                NeededAliases.Add(column.Id, column.Alias);
                            }

                            KataQuery.SelectRaw(sql + " AS " + column.Alias);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the SQL for grouping results.
        /// </summary>
        private void BuildGroupBySql()
        {
            // add chart grouping
            if (IsChart && DatasetColumns.ContainsKey(ChartRange.XAxisColumnId))
            {
                HasGrouping = true;
                KataQuery.GroupByRaw(DatasetColumns[ChartRange.XAxisColumnId].BuildSql(false));
                AddNeededTables(DatasetColumns[ChartRange.XAxisColumnId]);
            }
        }

        /// <summary>
        /// Build the from/join sql statements.
        /// </summary>
        private void BuildJoinSql()
        {
            var orderedJoins = new Dictionary<int, string>();
            foreach (var key in NeededTables.Keys)
            {
                if (key == Report.Dataset.PrimarySource)
                {
                    orderedJoins[0] = Report.Dataset.PrimarySource;
                }
                else
                {
                    orderedJoins[Joins[key].JoinOrder + 1] = key;
                }
            }
            // skip the primary source then start adding joins in the correct order
            orderedJoins.Skip(1).OrderBy(x => x.Key).Select(x => Joins[x.Value]).Each(x => KataQuery.Join(x.TableName, j => j.WhereRaw(x.Keys), $"{((JoinTypes)x.JoinTypeId).ToString().ToUpper()} JOIN"));
        }

        /// <summary>
        /// Build the SQL for the order by statement.
        /// </summary>
        private void BuildOrderBySql()
        {
            if (IsChart)
            {
                KataQuery.OrderByRaw("1");
                return;
            }

            var sortStrings = Report.ReportColumn.Where(c => c.SortOrder > 0).OrderBy(c => c.SortOrder).Where(x => DatasetColumns.ContainsKey(x.ColumnId)).ToList()
                .Select(x => {
                    var col = DatasetColumns[x.ColumnId];
                    var name = Database.AllowPaging ? col.Alias : col.BuildSql(false, 0);
                    return $"{name} {x.SortDirection.ToUpper()}";
                });

            if (sortStrings.Any())
            {
                KataQuery.OrderByRaw(sortStrings.Join());
            }
            else
            {
                KataQuery.OrderByRaw("1");
            }
        }

        /// <summary>
        /// Build the where statement for the sql query.
        /// </summary>
        private void BuildWhereSql()
        {
            if (!Report.Dataset.Conditions.IsEmpty())
            {
                KataQuery.WhereRaw(Report.Dataset.Conditions);
            }

            // iterate through all the report filters we have
            if (Report.ReportFilter != null && Report.ReportFilter.Count > 0)
            {
                Report.ReportFilter.Where(x => DatasetColumns.ContainsKey(x.ColumnId)).Each(x => {
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
                                KataQuery.WhereRaw($"{colAlias} IN ({x.Criteria.Delimit()})");
                                break;
                            case FilterOperatorsAbstract.NotIn:
                                // kata doesn't like passing a single quoted list, tries to escape em unless we do it this way
                                KataQuery.WhereRaw($"{colAlias} NOT IN ({x.Criteria.Delimit()})");
                                break;
                            case FilterOperatorsAbstract.Like:
                                KataQuery.WhereRaw($"{colAlias} LIKE ?", $"%{value1}%");
                                break;
                            case FilterOperatorsAbstract.NotLike:
                                KataQuery.WhereRaw($"{colAlias} NOT LIKE ?", $"%{value1}%");
                                break;
                            case FilterOperatorsAbstract.DateInterval:
                                // handle special date functions
                                var today = DateTime.Today;
                                var startDate = today;
                                var endDate = today;

                                switch ((FilterDateRanges)x.Criteria.ToInt())
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
                                KataQuery.WhereRaw($"{colAlias} BETWEEN ? AND ?", startDate.ToSqlDateTime(), endDate.ToSqlDateTime());
                                break;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Build parameter list for proc calls.
        /// </summary>
        private void BuildProcParams()
        {
            Report.ReportFilter.Where(x => DatasetColumns.ContainsKey(x.ColumnId)).Each(x => {
                var column = DatasetColumns[x.ColumnId];
                if (column.IsParam)
                {
                    Params.Add(column.ColumnName, column.IsDateTime ? x.Criteria.ToDateTime().ToSqlDateTime() : x.Criteria);
                }
            });
        }

        /// <summary>
        /// Check if a dataset column is being used to build a link for a column that is displayed in the report.
        /// </summary>
        /// <param name="column">Dataset column to check usage for.</param>
        /// <returns>Returns true if any links need this column.</returns>
        private bool UsedInLink(DatasetColumn column)
        {
            return DatasetColumns.Values.Any(x => ReportColumns.ContainsKey(x.Id) && !x.Link.IsEmpty() && x.Link.IndexOf(column.ColumnName, StringComparison.CurrentCultureIgnoreCase) > -1);
        }

        /// <summary>
        /// Builds a SQL query for a report or chart range.
        /// </summary>
        /// <param name="report">Report object to build the query for.</param>
        /// <param name="range">Change range to build the query for.</param>
        public QueryBuilder(Report report, ChartRange range = null)
        {
            Report = report;
            ChartRange = range;
            IsChart = range != null;
            Database = Report.Dataset.Database;
            DbType = (DatabaseTypes)Database.TypeId;

            Joins = report.Dataset.DatasetJoin?.ToDictionary(j => j.TableName, j => j) ?? new Dictionary<string, DatasetJoin>();
            DatasetColumns = report.Dataset.DatasetColumn?.ToDictionary(j => j.Id, j => j) ?? new Dictionary<int, DatasetColumn>();
            ReportColumns = Report.ReportColumn?.Where(j => DatasetColumns.ContainsKey(j.ColumnId)).ToDictionary(j => j.ColumnId, j => j) ?? new Dictionary<int, ReportColumn>();

            // add the primaryTable to the list of needed tables. we always query it
            NeededTables.Add(report.Dataset.PrimarySource, report.Dataset.PrimarySource);

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
        public Dictionary<int, string> NeededAliases { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> NeededColumns { get; set; } = new Dictionary<int, string>();
        public Query KataQuery { get; set; } = new Query();
        public SqlResult SqlResult { get; set; }

        /// <summary>
        /// Check if the query has any columns.
        /// </summary>
        /// <returns>True if there are columns selected in the query, else false.</returns>
        public bool HasColumns
        {
            get
            {
                return NeededColumns.Count > 0 || Report.Dataset.IsProc;
            }
        }

        public Dictionary<string, object> Params { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Get the statement to get the record count.
        /// </summary>
        public void CountStatement()
        {
            if (Report.Dataset.IsProc)
            {
                return;
            }

            if (Database.IsSqlServer)
            {
                SqlResult = new SqlServerCompiler { UseLegacyPagination = !Database.AllowPaging }.Compile(KataQuery.Clone().AsCount());
            }
            else
            {
                SqlResult = new MySqlCompiler().Compile(KataQuery.Clone().AsCount());
            }
        }

        /// <summary>
        /// Get the statement to execute a proc.
        /// </summary>
        /// <returns>Returns the SQL statement.</returns>
        public string ExecStatement()
        {
            return $"EXEC {Report.Dataset.PrimarySource} " + Params.Select(x => $"@{x.Key} = @{x.Key}").Join();
        }

        /// <summary>
        /// Build the complete SQL SELECT statement to get all columns. Optionally can be limited using start and rows.
        /// </summary>
        /// <param name="start">Row number to start at.</param>
        /// <param name="rows">Number of rows to return</param>
        public void SelectStatement(int start = 0, int rows = 0)
        {
            if (Report.Dataset.IsProc)
            {
                return;
            }

            if (rows > 0)
            {
                KataQuery.Limit(rows);
                KataQuery.Offset(start);
            }

            if (Database.IsSqlServer)
            {
                SqlResult = new SqlServerCompiler { UseLegacyPagination = !Database.AllowPaging }.Compile(KataQuery);
            }
            else
            {
                SqlResult = new MySqlCompiler().Compile(KataQuery);
            }
        }
    }
}
