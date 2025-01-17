﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    [ModelMetadataType(typeof(PasswordMetadata))]
    public class ResetPassword : BaseModel, IValidatableObject
    {
        UserMembership _Membership;

        [DbIgnore]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Hash { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string HelpText => PasswordHelper.HelpText(AppConfig);

        public bool IsReset { get; set; }

        [DbIgnore]
        public string Password { get; set; }

        public bool Reset(out string error)
        {
            error = "";
            try
            {
                var salt = Hasher.GenerateSalt();
                DbContext.Execute("UserPasswordSave", new { Id = _Membership.Id, Password = Hasher.HashPassword(Password, salt), Salt = salt, RequestUserId = _Membership.Id });
                DbContext.Execute("UserResetSave", new { _Membership.Id });
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, ex.Message);
                error = Account.ErrorSavingPassword;
            }
            return false;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            AppConfig = (IAppConfiguration)validationContext.GetService(typeof(IAppConfiguration));
            _Membership = DbContext.GetAll<UserMembership>(new { Email }).FirstOrDefault();
            if (_Membership == null || _Membership.ResetHash != Hash || _Membership.DateReset == null || _Membership.DateReset.Value < DateTimeOffset.Now.AddMinutes(-15))
            {
                _Membership = null;
                yield return new ValidationResult(Account.ErrorResetPassword);
            }

            if (IsReset)
                yield return PasswordHelper.Validate(AppConfig, Password, ConfirmPassword);
        }
    }
}
