using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Dash.I18n;
using Dash.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dash.Models
{
    public class LogOn : BaseModel
    {
        public LogOn()
        {
        }

        [Display(Name = "Password", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250), StringLength(250)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "UID", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string UserName { get; set; }

        /// <summary>
        /// Attempt to log the user on.
        /// </summary>
        /// <param name="error">Error message if any.</param>
        /// <returns>True on success, else false.</returns>
        public bool DoLogOn(out string error, IDbContext dbContext, HttpContext httpContext, ILogger<LogOn> logger)
        {
            error = "";
            try
            {
                var user = dbContext.GetAll<User>(new { UID = UserName, IsActive = true }).FirstOrDefault();
                if (user?.IsActive != true)
                {
                    error = Account.ErrorCannotValidate;
                }

                if (Hasher.VerifyPassword(user.Password, Password, user.Salt))
                {
                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, user.UID),
                        new Claim(ClaimTypes.PrimarySid, user.Id.ToString()),
                        new Claim("FullName", user.FullName)
                    };
                    claims.AddRange(dbContext.GetAll<UserClaim>(new { user.Id })
                        .Select(x => new Claim(ClaimTypes.Role, $"{x.ControllerName}.{x.ActionName}".ToLower())));

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties {
                        IsPersistent = true
                    };

                    var language = dbContext.Get<Language>(user.LanguageId);
                    if (language != null)
                    {
                        var cultureInfo = new CultureInfo(language.LanguageCode);
                        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                    }

                    httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    return true;
                }
                else
                {
                    error = Account.ErrorCannotValidate;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Account.ErrorCannotValidate);
                error = Account.ErrorCannotValidate;
            }
            return false;
        }
    }
}
