using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class ChartShare : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        public int RoleId { get; set; }
        public int UserId { get; set; }
    }
}
