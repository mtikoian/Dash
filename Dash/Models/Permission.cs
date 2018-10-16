namespace Dash.Models
{
    public class Permission : BaseModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }

        [DbIgnore]
        public string FullName { get { return ControllerName?.Trim() + "." + ActionName?.Trim(); } }
    }
}
