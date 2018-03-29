using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.I18n;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SaveChartShare : BaseModel, IValidatableObject
    {
        private Chart _Chart;

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Chart Chart { get { return _Chart ?? (_Chart = DbContext.Get<Chart>(Id)); } }

        public List<ChartShare> Shares { get; set; }

        public void Update()
        {
            Shares?.ForEach(x => { x.ChartId = Chart.Id; DbContext.Save(x); });
            Chart.ChartShare.Where(x => Shares?.Any(s => s.Id == x.Id) != true)
                .ToList()
                .ForEach(x => DbContext.Delete(x));
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
