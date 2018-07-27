﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class CopyAlert : BaseModel, IValidatableObject
    {
        private Alert _Alert;

        [BindNever, ValidateNever]
        public Alert Alert
        {
            get { return _Alert ?? (_Alert = DbContext.Get<Alert>(Id)); }
            set { _Alert = value; }
        }

        [Required(ErrorMessageResourceType = typeof(I18n.Reports), ErrorMessageResourceName = "ErrorNameRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Prompt { get; set; }

        public void Save()
        {
            Alert = Alert.Copy(Prompt);
            DbContext.Save(Alert);
            Id = Alert.Id;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Alert == null)
            {
                yield return new ValidationResult(I18n.Core.ErrorInvalidId);
            }
        }
    }
}