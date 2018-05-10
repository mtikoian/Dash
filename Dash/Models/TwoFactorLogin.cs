﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Utils;

namespace Dash.Models
{
    /// <summary>
    /// TwoFactorLogin is used only for user 2 factor login.
    /// </summary>
    public class TwoFactorLogin : BaseModel
    {
        [Required]
        public string AuthCode { get; set; }

        [Required]
        public string LoginHash { get; set; }

        public UserMembership Membership { get; set; }

        [Required]
        public string Username { get; set; }

        public bool Validate(out string error, IDbContext dbContext, IAppConfiguration appConfig)
        {
            error = "";
            Membership = dbContext.GetAll<UserMembership>(new { Username }).FirstOrDefault();
            if (Membership == null)
            {
                error = Account.ErrorCannotValidate;
                return false;
            }

            if (Membership.DateLogin < DateTimeOffset.Now || LoginHash != Membership.LoginHash)
            {
                error = Account.ErrorFactorExpired;
                return false;
            }

            var tfa = new TwoFactorAuthenticator();
            if (!tfa.ValidateTwoFactorPIN(appConfig.Membership.AuthenticatorKey, AuthCode))
            {
                error = Account.ErrorFactorInvalid;
                return false;
            }

            dbContext.Execute("UserLoginSave", new { Id = Membership.Id, LoginHash = (string)null, DateLogin = (DateTimeOffset?)null });
            // reload updated membership info
            Membership = dbContext.GetAll<UserMembership>(new { Username }).FirstOrDefault();
            return true;
        }
    }
}