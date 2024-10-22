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
        public long FilteredTotal { get; set; }
        public bool IsOwner { get; set; }
        public int Page { get; set; }
        public int ReportId { get; set; }
        public string ReportName { get; set; }
        public List<object> Rows { get; set; }
        public long Total { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
    }
}
