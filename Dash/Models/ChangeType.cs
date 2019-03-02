using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class ChangeType : BaseModel, IValidatableObject
    {
        private Chart _Chart;

        [BindNever, ValidateNever]
        public Chart Chart => _Chart ?? (_Chart = DbContext.Get<Chart>(Id));

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        public void Update()
        {
            Chart.ChartTypeId = ChartTypeId;
            Chart.Save();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Chart == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            else
            {
                if (!Chart.IsOwner)
                {
                    yield return new ValidationResult(Charts.ErrorOwnerOnly);
                }
                if (ChartTypeId < 1)
                {
                    yield return new ValidationResult(Charts.ErrorTypeRequired);
                }
            }
        }
    }
}
