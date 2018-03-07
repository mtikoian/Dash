using Dash.I18n;
using Dash.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Dash.Models
{
    public class LogOn : BaseModel
    {
        private IHttpContextAccessor HttpContextAccessor;

        public LogOn(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
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
        public bool DoLogOn(out string error)
        {
            error = "";
            try
            {
                var user = DbContext.GetAll<User>(new { UID = UserName, IsActive = true }).FirstOrDefault();
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
                    claims.AddRange(DbContext.GetAll<UserClaim>(new { user.Id })
                        .Select(x => new Claim(ClaimTypes.Role, $"{x.ControllerName}.{x.ActionName}".ToLower())));

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties {
                        IsPersistent = true
                    };

                    var language = DbContext.Get<Language>(user.LanguageId);
                    if (language != null)
                    {
                        var cultureInfo = new CultureInfo(language.LanguageCode);
                        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                    }

                    HttpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
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
                ex.Log(HttpContextAccessor);
                error = Account.ErrorCannotValidate;
            }
            return false;
        }
    }
}