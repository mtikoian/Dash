using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class ChartShare : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        public int RoleId { get; set; }
        public int UserId { get; set; }
    }
}
