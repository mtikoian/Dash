using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.I18n;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SaveRange : BaseModel, IValidatableObject
    {
        private Chart _Chart;

        public List<ChartRange> Ranges { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Chart Chart { get { return _Chart ?? (_Chart = DbContext.Get<Chart>(Id)); } }

        public List<ChartRange> Update()
        {
            return Chart.UpdateRanges(Ranges);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Chart == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            else if (!Chart.IsOwner)
            {
                yield return new ValidationResult(Charts.ErrorOwnerOnly);
            }
        }
    }
}
