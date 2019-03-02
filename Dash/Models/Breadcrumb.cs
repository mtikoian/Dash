namespace Dash.Models
{
    public class Breadcrumb
    {
        public Breadcrumb(string label, string action, string controller, object routeValues = null, bool hasAccess = true)
        {
            Label = label;
            Controller = controller;
            Action = action;
            RouteValues = routeValues;
            HasAccess = hasAccess;
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public bool HasAccess { get; set; }
        public string Label { get; set; }
        public object RouteValues { get; set; }
    }
}
