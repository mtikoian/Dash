using System;

namespace Dash.Models
{
    public class ErrorLog : BaseModel
    {
        public string Host { get; set; }
        public string Message { get; set; }
        public string Method { get; set; }
        public string Namespace { get; set; }
        public string Path { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Type { get; set; }
        public string User { get; set; }
    }
}
