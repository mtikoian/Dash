using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Dash.Utils;

namespace Dash.Models
{
    public class ResetPassword : BaseModel, IValidatableObject
    {
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Users))]
        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [Ignore]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Hash { get; set; }

        public bool IsReset { get; set; }

        [Display(Name = "Password", ResourceType = typeof(Users))]
        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Ignore]
        public string Password { get; set; }

        private UserMembership Membership { get; set; }

        /// <summary>
        /// Reset a user's password.
        /// </summary>
        /// <param name="error">Error message if any.</param>
        /// <returns>True on success, else false.</returns>
        public bool Reset(out string error)
        {
            error = "";
            try
            {
                var salt = Hasher.GenerateSalt();
                DbContext.Execute("UserPasswordSave", new { Id = Membership.Id, Password = Hasher.HashPassword(Password, salt), Salt = salt, RequestUserId = Membership.Id });
                DbContext.Execute("UserResetSave", new { Membership.Id });
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, ex.Message);
                error = Account.ErrorSavingPassword;
            }
            return false;
        }

        /// <summary>
        /// Validate object.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            AppConfig = (AppConfiguration)validationContext.GetService(typeof(AppConfiguration));
            Membership = DbContext.GetAll<UserMembership>(new { Email }).FirstOrDefault();
            if (Membership == null || Membership.ResetHash != Hash || Membership.DateReset == null || Membership.DateReset.Value < DateTimeOffset.Now.AddMinutes(-15))
            {
                Membership = null;
                yield return new ValidationResult(Account.ErrorResetPassword);
            }

            if (IsReset)
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(Account.ErrorPasswordMatch, new[] { "ConfirmPassword" });
                }
                else if ((Password.Length < AppConfig.Membership.MinRequiredPasswordLength) || Password.ToCharArray().Count(c => !Char.IsLetterOrDigit(c)) < AppConfig.Membership.MinRequiredNonAlphanumericCharacters)
                {
                    yield return new ValidationResult(Account.ErrorInvalidPassword, new[] { "Password" });
                }
            }
        }
    }
}
