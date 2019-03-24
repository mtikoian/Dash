using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class CreateChart : BaseModel
    {
        public CreateChart() { }

        public CreateChart(IDbContext dbContext, int userId)
        {
            DbContext = dbContext;
            RequestUserId = userId;
        }

        [Display(Name = "Type", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartTypeId { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength"), StringLength(100)]
        public string Name { get; set; }
    }
}
