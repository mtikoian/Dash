using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class CopyChart : BaseModel, IValidatableObject
    {
        private Chart _Chart;

        [BindNever, ValidateNever]
        public Chart Chart
        {
            get { return _Chart ?? (_Chart = DbContext.Get<Chart>(Id)); }
            set { _Chart = value; }
        }

        [Required(ErrorMessageResourceType = typeof(I18n.Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public void Save()
        {
            Chart = Chart.Copy(Prompt);
            DbContext.Save(Chart);
            Id = Chart.Id;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Chart == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
        }
    }
}
