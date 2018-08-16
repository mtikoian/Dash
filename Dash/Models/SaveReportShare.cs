using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SaveReportShare : BaseModel, IValidatableObject
    {
        private Report _Report;

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); } }

        public List<ReportShare> Shares { get; set; }

        public void Update()
        {
            Shares?.ForEach(x => { x.ReportId = Report.Id; DbContext.Save(x); });
            Report.ReportShare.Where(x => Shares?.Any(s => s.Id == x.Id) != true)
                .ToList()
                .ForEach(x => DbContext.Delete(x));
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
