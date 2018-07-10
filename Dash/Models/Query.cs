using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// This class is designed to build and hold the pieces of the sql query for a report.
    /// </summary>
    public class Query
    {
        public Dictionary<int, DatasetColumn> DatasetColumns = new Dictionary<int, DatasetColumn>();
        public Dictionary<int, string> NeededAliases = new Dictionary<int, string>();
        public Dictionary<int, string> NeededColumns = new Dictionary<int, string>();
        private ChartRange ChartRange;
        private string ColumnSql = "";
        private Database Database;
        private string GroupBySql = "";
        private Dictionary<int, ReportGroup> GroupColumns = new Dictionary<int, ReportGroup>();
        private List<string> Grouping = new List<string>();
        private bool IsChart;
        private Dictionary<string, DatasetJoin> Joins = new Dictionary<string, DatasetJoin>();
        private string JoinSql = "";
        private Dictionary<string, string> NeededTables = new Dictionary<string, string>();
        private string OrderBySql = "";
        private Dictionary<string, object> Parameters = new Dictionary<string, object>();
        private Report Report;
        private Dictionary<int, ReportColumn> ReportColumns = new Dictionary<int, ReportColumn>();
        private string WhereSql = "";

        /// <summary>
        /// Builds a SQL query for a report.
        /// </summary>
        /// <param name="report">Report object to build the query for.</param>
        /// <param name="isChart">Set if this query will be used for creating a chart, default to false.</param>
        public Query(Report report, ChartRange range = null)
        {
            Report = report;
            ChartRange = range;
            IsChart = range != null;
            Database = Report.Dataset.Database;

            // make a dictionary of joins for this dataset
            if (report.Dataset.DatasetJoin != null)
            {
                Joins = report.Dataset.DatasetJoin.ToDictionary(j => j.TableName, j => j);
            }

            // make a dictionary of columns for this dataset
            if (report.Dataset.DatasetColumn != null)
            {
                DatasetColumns = report.Dataset.DatasetColumn.ToDictionary(j => j.Id, j => j);
            }

            // make a dictionary of columns for this report
            if (Report.ReportColumn != null)
            {
                ReportColumns = Report.ReportColumn.Where(j => DatasetColumns.ContainsKey(j.ColumnId)).ToDictionary(j => j.ColumnId, j => j);
            }

            // make a dictionary of all columns used for grouping
            if (Report.ReportGroup != null)
            {
                GroupColumns = Report.ReportGroup.Where(j => DatasetColumns.ContainsKey(j.ColumnId)).ToDictionary(j => j.ColumnId, j => j);
            }

            // add the primaryTable to the list of needed tables. we always query it
            NeededTables.Add(report.Dataset.PrimarySource, report.Dataset.PrimarySource);

            // make the group by statement
            BuildGroupBySql();

            // build the columns
            BuildColumnSql();

            // build the where
            BuildWhereSql();

            // build the sorting
            BuildOrderBySql();

            // build the joins
            BuildJoinSql();
        }

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

        public Dictionary<string, object> Params
        {
            get
            {
                return Parameters;
            }
        }

        /// <summary>
        /// Get the statement to get the record count.
        /// </summary>
        /// <param name="prepare">Prepare the SQL statment by replacing parameters.</param>
        /// <returns>Returns the SQL statement.</returns>
        public string CountStatement(bool prepare = false)
        {
            var querySql = "";

            if (Grouping.Count > 0)
            {
                if (Database.IsSqlServer)
                {
                    querySql = $"SELECT COUNT(1) OVER() AS cnt {JoinSql.AddLine()} {WhereSql.AddLine()} {GroupBySql.AddLine()}";
                }
                else
                {
                    querySql = $"SELECT COUNT(cnt) AS cnt FROM (SELECT COUNT(1) AS cnt {JoinSql.AddLine()} {WhereSql.AddLine()} {GroupBySql.AddLine()}) c";
                }
            }
            else
            {
                querySql = $"SELECT COUNT(1) AS cnt {JoinSql.AddLine()} {WhereSql.AddLine()} {GroupBySql.AddLine()}";
            }

            if (prepare)
            {
                querySql = PrepareSql(querySql);
            }

            return querySql;
        }

        /// <summary>
        /// Get the statement to execute a proc.
        /// </summary>
        /// <param name="prepare">Prepare the SQL statment by replacing parameters.</param>
        /// <returns>Returns the SQL statement.</returns>
        public string ExecStatement(bool prepare = false)
        {
            var querySql = $"EXEC {Report.Dataset.PrimarySource}";
            if (prepare)
            {
                querySql = PrepareSql(querySql, true);
            }
            return querySql;
        }

        /// <summary>
        /// Build the complete SQL SELECT statement to get all columns. Optionally can be limited using start and rows.
        /// </summary>
        /// <param name="start">Row number to start at.</param>
        /// <param name="rows">Number of rows to return</param>
        /// <param name="prepare">Prepare the SQL statment by replacing parameters.</param>
        /// <returns>Returns the SQL SELECT ready to run.</returns>
        public string SelectStatement(int start = 0, int rows = 0, bool prepare = false)
        {
            var querySql = $"SELECT {ColumnSql.AddLine()} {JoinSql.AddLine()} {WhereSql.AddLine()} {GroupBySql.AddLine()} {OrderBySql.AddLine()}";

            if (rows > 0)
            {
                // tack on the row limit if the database allows it
                if (!Database.IsSqlServer)
                {
                    querySql += $" LIMIT {rows} OFFSET {start}";
                }
                else if (Database.AllowPaging)
                {
                    querySql += $" OFFSET {start} ROWS FETCH NEXT {rows} ROWS ONLY";
                }
                else
                {
                    querySql = $"SELECT {string.Join(", ", NeededAliases.Values)} FROM \n(SELECT {ColumnSql}, ROW_NUMBER() OVER ({OrderBySql}) AS RowNum \n{JoinSql.AddLine()} {WhereSql.AddLine()} {GroupBySql.AddLine()}) a WHERE RowNum > {start} AND RowNum <= {start + rows}";
                }
            }

            if (prepare)
            {
                querySql = PrepareSql(querySql);
            }

            return querySql;
        }

        /// <summary>
        /// Build a list of joins necessary to include the requested table in a query.
        /// </summary>
        /// <param name="table">Table name to get joins for.</param>
        /// <returns></returns>
        private Dictionary<string, string> AddJoin(Dictionary<string, string> tables, string table)
        {
            if (Joins == null || !Joins.ContainsKey(table))
            {
                return tables;
            }
            if (tables.ContainsKey(Joins[table].TableName))
            {
                return tables;
            }

            var keys = Joins[table].Keys;
            tables.Add(Joins[table].TableName, $"{((JoinTypes)Joins[table].JoinTypeId).ToString().ToUpper()} JOIN {Joins[table].TableName} ON {keys}");

            // check if we need to add any tables to get the keys to join the current table
            foreach (var join in Joins.Values)
            {
                if (keys.IndexOf(join.TableName + ".") > -1 && !tables.ContainsKey(join.TableName))
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
                var table = column.Table;
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

            if (Report.Dataset.IsProc)
            {
                return;
            }

            // if its a chart, we just need x and y columns
            if (IsChart)
            {
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
                    }
                }
                else
                {
                    // if no y axis, just use the primary key
                    sql += "(1)";
                    // add the column to the list
                    NeededColumns.Add(0, sql + " AS yValue");
                }
            }
            else
            {
                // build the columns for the query
                if (GroupBySql.Length > 0 && Report.AggregatorId == 0)
                {
                    Report.AggregatorId = (int)Aggregators.Count;
                }

                // iterate through all the columns in the dataset and see which ones we actually need
                foreach (var column in Report.Dataset.DatasetColumn)
                {
                    if (!NeededColumns.ContainsKey(column.Id) && (ReportColumns.ContainsKey(column.Id) || UsedInLink(column)))
                    {
                        // build the sql to get this column
                        sql = "";

                        if (GroupBySql.Length > 0 && !GroupColumns.ContainsKey(column.Id))
                        {
                            sql = column.BuildSql(false, Report.AggregatorId);
                        }
                        else if (GroupColumns.ContainsKey(column.Id))
                        {
                            sql = column.BuildSql(false, (int)Aggregators.Max);
                        }
                        else
                        {
                            sql = column.BuildSql(false);
                        }

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
                        }
                    }
                }
            }

            ColumnSql = NeededColumns.Count > 0 ? string.Join(", ", NeededColumns.Values.ToArray()) : "";
        }

        /// <summary>
        /// Creates the SQL for grouping results.
        /// </summary>
        private void BuildGroupBySql()
        {
            Grouping = new List<string>();

            if (Report.Dataset.IsProc)
            {
                return;
            }

            if (Report.ReportGroup != null && Report.ReportGroup.Count > 0)
            {
                foreach (var group in Report.ReportGroup)
                {
                    if (DatasetColumns.ContainsKey(group.ColumnId))
                    {
                        var sql = DatasetColumns[group.ColumnId].BuildSql(false);
                        if (sql.Length > 0)
                        {
                            Grouping.Add(sql);
                            AddNeededTables(DatasetColumns[group.ColumnId]);
                        }
                    }
                }
            }

            // add chart grouping
            if (IsChart && DatasetColumns.ContainsKey(ChartRange.XAxisColumnId))
            {
                Grouping.Add(DatasetColumns[ChartRange.XAxisColumnId].BuildSql(false));
                AddNeededTables(DatasetColumns[ChartRange.XAxisColumnId]);
            }

            GroupBySql = Grouping.Count > 0 ? " GROUP BY " + Grouping.Join() : "";
        }

        /// <summary>
        /// Build the from/join sql statements.
        /// </summary>
        private void BuildJoinSql()
        {
            if (Report.Dataset.IsProc)
            {
                return;
            }

            var orderedJoins = new Dictionary<int, string>();
            foreach (var key in NeededTables.Keys)
            {
                if (key == Report.Dataset.PrimarySource)
                {
                    orderedJoins[0] = Report.Dataset.PrimarySource;
                }
                else
                {
                    orderedJoins[Joins[key].JoinOrder + 1] = NeededTables[key];
                }
            }
            JoinSql = " FROM " + orderedJoins.OrderBy(x => x.Key).Select(x => x.Value).Join(" \n ");
        }

        /// <summary>
        /// Build the SQL for the order by statement.
        /// </summary>
        private void BuildOrderBySql()
        {
            if (Report.Dataset.IsProc)
            {
                return;
            }

            if (IsChart)
            {
                OrderBySql = " ORDER BY 1 ";
                return;
            }

            var sortedCols = Report.ReportColumn.Where(c => c.SortOrder > 0).OrderBy(c => c.SortOrder);
            if (sortedCols.Any())
            {
                sortedCols = sortedCols.OrderBy(x => x.SortOrder);
                var sortStrings = sortedCols.Where(x => DatasetColumns.ContainsKey(x.ColumnId)).ToList()
                    .Select(x =>
                    {
                        var col = DatasetColumns[x.ColumnId];
                        var name = Database.AllowPaging ? col.Alias : col.BuildSql(false, Report.ReportGroup.Count > 0 ? Report.AggregatorId : 0);
                        return $"{name} {x.SortDirection.ToUpper()}";
                    });

                if (sortStrings.Any())
                {
                    OrderBySql = " ORDER BY " + sortStrings.Join();
                }
            }
            // set the default sort
            if (OrderBySql.IsEmpty())
            {
                OrderBySql = " ORDER BY 1";
            }
        }

        /// <summary>
        /// Build the where statement for the sql query.
        /// </summary>
        private void BuildWhereSql()
        {
            WhereSql = Report.Dataset.Conditions;

            // iterate through all the report filters we have
            if (Report.ReportFilter != null && Report.ReportFilter.Count > 0)
            {
                var filterSql = new Dictionary<int, string>();
                foreach (var filter in Report.ReportFilter)
                {
                    if (DatasetColumns.ContainsKey(filter.ColumnId))
                    {
                        // build the sql for this piece
                        var sql = filter.BuildFilterSql(DatasetColumns[filter.ColumnId], filter, out var newParameters);
                        if (DatasetColumns[filter.ColumnId].IsParam)
                        {
                            Parameters.AddRange(newParameters);
                        }
                        else if (sql.Trim().Length > 0)
                        {
                            Parameters.AddRange(newParameters);

                            // if there are multiple filters for one column OR them together
                            if (filterSql.ContainsKey(filter.ColumnId))
                            {
                                filterSql[filter.ColumnId] += " OR ";
                            }
                            else
                            {
                                filterSql[filter.ColumnId] = "";
                            }
                            filterSql[filter.ColumnId] += sql;

                            // add this table for this column
                            AddNeededTables(DatasetColumns[filter.ColumnId]);
                        }
                    }
                }

                // add all the filters into the where statement
                if (filterSql.Count > 0)
                {
                    WhereSql += " " + filterSql.ToList().Select(x => $"({x.Value})").Join(" \n AND ");
                }
            }

            if (!WhereSql.IsEmpty())
            {
                WhereSql = " WHERE " + WhereSql;
            }
        }

        /// <summary>
        /// Replace parameter names with escaped values.
        /// </summary>
        /// <param name="sql">SQL query to prepare.</param>
        /// <param name="isProc">SQL is for a stored proc instead of a table query.</param>
        /// <returns>Returns updated SQL statement.</returns>
        private string PrepareSql(string sql, bool isProc = false)
        {
            if (isProc)
            {
                return sql + " " + Parameters.ToList().Select(x => $"@{x.Key} = '{x.Value.ToString().Replace("'", "''")}'").Join(", ");
            }
            Parameters.ToList().ForEach(x => sql = sql.Replace(x.Key, $"'{x.Value.ToString().Replace("'", "''")}'"));
            return sql;
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
    }
}
