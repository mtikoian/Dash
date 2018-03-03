using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// DatasetRole is the link between a role and a dataset.
    /// </summary>
    public class DatasetRole : BaseModel
    {
        [Required]
        public int DatasetId { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}