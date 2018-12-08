using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;

namespace Dash.Models
{
    public static class PasswordHelper
    {
        public static string HelpText(IAppConfiguration appConfig)
        {
            return Users.PasswordHelp.Replace("{0}", appConfig.Membership.MinRequiredPasswordLength.ToString())
                    .Replace("{1}", appConfig.Membership.MinRequiredNonAlphanumericCharacters.ToString());
        }

        public static ValidationResult Validate(IAppConfiguration appConfig, string password, string confirmPassword)
        {
            if (password.IsEmpty() || confirmPassword.IsEmpty())
            {
                return new ValidationResult(Account.ErrorInvalidPassword, new[] { "Password" });
            }
            else if (password != confirmPassword)
            {
                return new ValidationResult(Account.ErrorPasswordMatch, new[] { "ConfirmPassword" });
            }
            else if (password.Length < appConfig.Membership.MinRequiredPasswordLength ||
                password.ToCharArray().Count(c => !Char.IsLetterOrDigit(c)) < appConfig.Membership.MinRequiredNonAlphanumericCharacters)
            {
                return new ValidationResult(Account.ErrorInvalidPassword, new[] { "Password" });
            }
            return null;
        }
    }

    public abstract class PasswordMetadata
    {
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Users))]
        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Password", ResourceType = typeof(Users))]
        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }
    }
}
