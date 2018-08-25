﻿using System;
using System.Collections.Generic;

namespace Dash.Models
{
    public class ReportResult
    {
        public string CountError { get; set; }
        public string CountSql { get; set; }
        public string DataError { get; set; }
        public string DataSql { get; set; }
        public string Error { get; set; }
        public int FilteredTotal { get; set; }
        public int Page { get; set; }
        public int ReportId { get; set; }
        public List<object> Rows { get; set; }
        public int Total { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
    }
}
