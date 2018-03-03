using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// Share a report with a user or role.
    /// </summary>
    public class ReportShare : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        public int RoleId { get; set; }
        public int UserId { get; set; }
    }
}