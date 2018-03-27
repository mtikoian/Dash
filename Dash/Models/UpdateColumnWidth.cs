using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.I18n;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class UpdateColumnWidth : BaseModel, IValidatableObject
    {
        private Report _Report;

        public List<TableColumnWidth> Columns { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); } }

        public decimal ReportWidth { get; set; }

        public void UpdateColumns()
        {
            Report.UpdateColumnWidths(ReportWidth, Columns);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Report == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            else
            {
                if (!Report.IsOwner)
                {
                    yield return new ValidationResult(Reports.ErrorOwnerOnly);
                }
                if (Columns?.Any() != true || !Report.ReportColumn.Any())
                {
                    yield return new ValidationResult(Reports.ErrorNoColumnsSelected);
                }
            }
        }
    }
}
