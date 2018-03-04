namespace Dash.Models
{
    /// <summary>
    /// UserClaim is used to map permission from the db into claims for the identity.
    /// </summary>
    public class UserClaim : BaseModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
    }
}