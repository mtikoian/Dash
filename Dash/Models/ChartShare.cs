using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class ChartShare : BaseModel
    {
        private Chart _Chart;

        public ChartShare()
        {
        }

        public ChartShare(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ChartShare(IDbContext dbContext, int chartId)
        {
            DbContext = dbContext;
            ChartId = chartId;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        [Ignore, BindNever, ValidateNever]
        public string ChartName { get { return Chart?.Name; } }

        public int? RoleId { get; set; }

        [Ignore]
        public string RoleName { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> RoleSelectListItems
        {
            get
            {
                return DbContext.GetAll<Role>().OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Id.ToString());
            }
        }

        [Ignore]
        public string UserFirstName { get; set; }
        public int? UserId { get; set; }

        [Ignore]
        public string UserLastName { get; set; }

        [Ignore]
        public string UserName { get { return $"{UserLastName?.Trim()}, {UserFirstName?.Trim()}".Trim(new char[] { ' ', ',' }); } }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> UserSelectListItems
        {
            get
            {
                return DbContext.GetAll<User>().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToSelectList(x => x.FullName, x => x.Id.ToString());
            }
        }

        private Chart Chart { get { return _Chart ?? (_Chart = DbContext.Get<Chart>(ChartId)); } }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!UserId.HasValue && !RoleId.HasValue)
            {
                yield return new ValidationResult(Core.ErrorUserOrRole, new[] { "UserID" });
            }
        }
    }
}
