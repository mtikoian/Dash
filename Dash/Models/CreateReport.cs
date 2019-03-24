using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;

namespace Dash.Models
{
    public class CreateReport : BaseModel, IValidatableObject
    {
        public CreateReport() { }

        public CreateReport(IDbContext dbContext, int userId)
        {
            DbContext = dbContext;
            RequestUserId = userId;
        }

        [Display(Name = "Dataset", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength"), StringLength(250)]
        public string Name { get; set; }

        public IEnumerable<Dataset> GetDatasetsForUser() => DbContext.GetAll<Dataset>(new { UserId = RequestUserId });

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (DbContext.Get<User>(RequestUserId.Value)?.CanAccessDataset(DatasetId) != true)
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
        }
    }
}
