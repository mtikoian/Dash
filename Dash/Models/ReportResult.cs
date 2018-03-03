using System;
using System.Collections.Generic;

namespace Dash.Models
{
    /// <summary>
    /// ReportResult stores results from a report query.
    /// </summary>
    public class ReportResult
    {
        public string CountError { get; set; }
        public string CountSql { get; set; }
        public string DataError { get; set; }
        public string DataSql { get; set; }
        public string Error { get; set; }
        public int FilteredTotal { get; set; }
        public int Page { get; set; }
        public List<object> Rows { get; set; }
        public int Total { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
    }
}