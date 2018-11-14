using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class LogOn : BaseModel
    {
        public UserMembership Membership { get; private set; }

        [Display(Name = "Password", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250), StringLength(250)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "UserName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string UserName { get; set; }

        public bool DoLogOn(out string error, IDbContext dbContext, IAppConfiguration appConfig, HttpContext httpContext)
        {
            error = "";
            try
            {
                Membership = dbContext.GetAll<UserMembership>(new { UserName }).FirstOrDefault();
                if (Membership.DateUnlocks > DateTimeOffset.Now)
                {
                    error = string.Format(Account.ErrorAccountLocked, appConfig.Membership.LoginAttemptsLockDuration);
                    return false;
                }

                if (!Hasher.VerifyPassword(Membership.Password, Password, Membership.Salt))
                {
                    // if the user's lock period has expired but they still entered the wrong password reset the count, otherwise increment the count
                    Membership.LoginAttempts = Membership.LoginAttempts > appConfig.Membership.MaxLoginAttempts ? 1 : Membership.LoginAttempts + 1;
                    if (Membership.LoginAttempts > appConfig.Membership.MaxLoginAttempts)
                    {
                        Membership.DateUnlocks = DateTimeOffset.Now.AddMinutes(appConfig.Membership.LoginAttemptsLockDuration);
                        error = string.Format(Account.ErrorAccountLocked, appConfig.Membership.LoginAttemptsLockDuration);
                    }
                    else
                    {
                        Membership.DateUnlocks = DateTimeOffset.MinValue;
                        error = Account.ErrorCannotValidate;
                    }

                    dbContext.Execute("UserLoginAttemptsSave", new { Membership.Id, Membership.LoginAttempts, Membership.DateUnlocks });
                    return false;
                }

                if (Membership.AllowSingleFactor)
                {
                    Membership.DoLogOn(dbContext, httpContext);
                }
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, Account.ErrorCannotValidate);
                error = Account.ErrorCannotValidate;
            }
            return false;
        }
    }
}
