using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class RenameChart : BaseModel, IValidatableObject
    {
        Chart _Chart;
        IHttpContextAccessor _HttpContextAccessor;

        [BindNever, ValidateNever]
        public Chart Chart
        {
            get => _Chart ?? (_Chart = DbContext.Get<Chart>(Id));
            set => _Chart = value;
        }

        [Display(Name = "Name", ResourceType = typeof(Charts))]
        [Required(ErrorMessageResourceType = typeof(Charts), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        public void Save()
        {
            Chart.Name = Name.Trim();
            Chart.Save(false);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));
            if (Chart == null)
                yield return new ValidationResult(Core.ErrorInvalidId);
        }
    }
}
