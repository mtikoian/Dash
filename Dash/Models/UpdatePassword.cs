using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Configuration;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    [ModelMetadataType(typeof(PasswordMetadata))]
    public class UpdatePassword : BaseModel, IValidatableObject
    {
        public UpdatePassword()
        {
        }

        public UpdatePassword(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        [DbIgnore]
        public string ConfirmPassword { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string HelpText => PasswordHelper.HelpText(AppConfig);

        [DbIgnore]
        public string Password { get; set; }

        public bool Save(out string error)
        {
            error = "";
            try
            {
                var salt = Hasher.GenerateSalt();
                DbContext.Execute("UserPasswordSave", new { Id = RequestUserId, Password = Hasher.HashPassword(Password, salt), Salt = salt, RequestUserId = RequestUserId });
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
            yield return PasswordHelper.Validate(AppConfig, Password, ConfirmPassword);
        }
    }
}
