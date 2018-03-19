namespace Dash.Models
{
    /// <summary>
    /// Permission is a combination of action and controller, used for user authorization. AuthActionFilter checks permissions.
    /// </summary>
    public class Permission : BaseModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }

        [Ignore]
        public string FullName { get { return ControllerName?.Trim() + "." + ActionName?.Trim(); } }
    }
}