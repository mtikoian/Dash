using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class SelectColumn : BaseModel, IValidatableObject
    {
        const int MaxColumns = 20;
        IHttpContextAccessor _HttpContextAccessor;
        Report _Report;

        public bool AllowCloseParent { get; set; }

        public List<ReportColumn> Columns { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public new int Id { get; set; }

        [BindNever, ValidateNever]
        public Report Report => _Report ?? (_Report = DbContext.Get<Report>(Id));

        public void Update(int? userId = null) => Report.UpdateColumns(Columns.Where(x => x.DisplayOrder > 0).ToList(), userId);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));

            var count = Columns?.Count(x => x.DisplayOrder > 0) ?? 0;
            if (count == 0)
                yield return new ValidationResult(Reports.ErrorMinColumns);
            if (count > MaxColumns)
                yield return new ValidationResult(Reports.ErrorMaxColumns);
            if (Report == null)
                yield return new ValidationResult(Core.ErrorInvalidId);
            if (!Report.IsOwner)
                yield return new ValidationResult(Reports.ErrorOwnerOnly);
            if (DbContext.Get<User>(_HttpContextAccessor.HttpContext.User.UserId())?.CanAccessDataset(Report.DatasetId) != true)
                yield return new ValidationResult(Reports.ErrorReportDatasetAccess);
        }
    }
}
