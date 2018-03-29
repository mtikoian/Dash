using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class CreateChart : BaseModel
    {
        public CreateChart()
        {
        }

        public CreateChart(IDbContext dbContext, int userId)
        {
            DbContext = dbContext;
            RequestUserId = userId;
        }

        [Display(Name = "Type", ResourceType = typeof(I18n.Charts))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartTypeId { get; set; }

        [Display(Name = "Name", ResourceType = typeof(I18n.Charts))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength"), StringLength(100)]
        public string Name { get; set; }
    }
}
