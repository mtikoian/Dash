﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Dash.I18n;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class UserMembership : BaseModel
    {
        public bool AllowSingleFactor { get; set; }

        [Display(Name = "AuthCode", ResourceType = typeof(Account))]
        public string AuthCode { get; set; }
        public DateTimeOffset? DateLogin { get; set; }
        public DateTimeOffset? DateUnlocks { get; set; }
        public DateTimeOffset? DateReset { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string FullName { get { return $"{FirstName.Trim()} {LastName}".Trim(); } }
        public bool IsActive { get; set; }
        public int LanguageId { get; set; }
        public string LastName { get; set; }
        public int LoginAttempts { get; set; }
        public string LoginHash { get; set; }
        public string ResetHash { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string UserName { get; set; }

        public string CreateHash()
        {
            LoginHash = Guid.NewGuid().ToString();
            // @todo rename to something like DateLoginWindow to make more sense
            DbContext.Execute("UserLoginSave", new { Id = Id, LoginHash = LoginHash, DateLogin = DateTimeOffset.Now.AddMinutes(5) });
            return LoginHash;
        }

        public void DoLogOn(IDbContext dbContext, HttpContext httpContext)
        {
            dbContext.Execute("UserLoginAttemptsSave", new {
                Id = Id, LoginAttempts = 0, DateUnlocks = DateTimeOffset.MinValue
            });

            var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, UserName),
                        new Claim(ClaimTypes.PrimarySid, Id.ToString()),
                        new Claim("FullName", FullName)
                    };
            claims.AddRange(dbContext.GetAll<UserClaim>(new { Id })
                .Select(x => new Claim(ClaimTypes.Role, $"{x.ControllerName}.{x.ActionName}".ToLower())));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties {
                IsPersistent = true
            };

            var language = dbContext.Get<Language>(LanguageId);
            if (language != null)
            {
                var cultureInfo = new CultureInfo(language.LanguageCode);
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            }

            httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}