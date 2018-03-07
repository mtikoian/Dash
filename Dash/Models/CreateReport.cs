using Dash.I18n;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace Dash.Models
{
    public class CreateReport : BaseModel, IValidatableObject
    {
        private IHttpContextAccessor HttpContextAccessor;

        public CreateReport(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        [Display(Name = "Dataset", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength"), StringLength(250)]
        public string Name { get; set; }

        /// <summary>
        /// Get all datasets a user can access.
        /// </summary>
        /// <returns>Returns a dictionary of <RoleId, DatasetRole>.</returns>
        public IEnumerable<Dataset> GetDatasetsForUser(int userId)
        {
            return DbContext.GetAll<Dataset>(new { UserId = userId });
        }

        /// <summary>
        /// Validate report object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var user = DbContext.Get<User>(HttpContextAccessor.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanAccessDataset(DatasetId))
            {
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
            }
        }
    }
}