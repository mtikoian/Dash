using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SaveGroup : BaseModel, IValidatableObject
    {
        private Report _Report;

        public List<ReportGroup> Groups { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); } }

        public int GroupAggregator { get; set; }

        public List<ReportGroup> Update()
        {
            return Report.UpdateGroups(GroupAggregator, Groups);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Report == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            else if (!Report.IsOwner)
            {
                yield return new ValidationResult(Reports.ErrorOwnerOnly);
            }
        }
    }
}
