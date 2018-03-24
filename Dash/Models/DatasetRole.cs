using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class DatasetRole : BaseModel
    {
        [Required]
        public int DatasetId { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}
