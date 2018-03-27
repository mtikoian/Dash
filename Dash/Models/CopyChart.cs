using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    /// <summary>
    /// CopyChart is used to copy a chart.
    /// </summary>
    public class CopyChart : BaseModel, IValidatableObject
    {
        private Chart _Chart;

        public Chart Chart { get { return _Chart ?? (_Chart = DbContext.Get<Chart>(Id)); } }

        public Chart NewChart { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        /// <summary>
        /// Save the chart.
        /// </summary>
        public void Save()
        {
            NewChart = Chart.Copy(Prompt);
            DbContext.Save(NewChart);
        }

        /// <summary>
        /// Validate object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>Returns a list of validation errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Chart == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
        }
    }
}
