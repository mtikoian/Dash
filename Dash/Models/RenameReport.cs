﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class RenameReport : BaseModel, IValidatableObject
    {
        private IHttpContextAccessor _HttpContextAccessor;
        private Report _Report;

        [Display(Name = "Name", ResourceType = typeof(Reports))]
        [Required(ErrorMessageResourceType = typeof(Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [BindNever, ValidateNever]
        public Report Report
        {
            get { return _Report ?? (_Report = DbContext.Get<Report>(Id)); }
            set { _Report = value; }
        }

        public void Save()
        {
            Report.Name = Name.Trim();
            DbContext.Save(Report, false);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));
            if (Report == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
        }
    }
}