using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.I18n;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SelectColumn : BaseModel, IValidatableObject
    {
        private IHttpContextAccessor _HttpContextAccessor;
        private Report _Report;

        public bool AllowCloseParent { get; set; }

        public List<ReportColumn> Columns { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); } }

        public void UpdateColumns()
        {
            Report.UpdateColumns(Columns.Where(x => x.DisplayOrder > 0).ToList());
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));

            if (Columns?.Any(x => x.DisplayOrder > 0) != true)
            {
                yield return new ValidationResult(Reports.ErrorSelectColumn);
            }
            if (Report == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
            if (!Report.IsOwner)
            {
                yield return new ValidationResult(Reports.ErrorOwnerOnly);
            }
            var user = DbContext.Get<User>(_HttpContextAccessor.HttpContext.User.UserId());
            if (user?.CanAccessDataset(Report.DatasetId) != true)
            {
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
            }
        }
    }
}
