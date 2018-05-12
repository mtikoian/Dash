using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using MimeKit;

namespace Dash.Models
{
    public class ForgotPassword : BaseModel
    {
        [Display(Name = "UserName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string UserName { get; set; }

        public bool Send(out string error, IDbContext dbContext, HttpContext httpContext, IAppConfiguration appConfig, UrlHelper urlHelper)
        {
            error = "";
            try
            {
                var myUser = dbContext.GetAll<User>(new { UserName }).FirstOrDefault();
                if (myUser != null)
                {
                    var hash = Guid.NewGuid().ToString();
                    dbContext.Execute("UserResetSave", new { Id = myUser.Id, ResetHash = hash, DateReset = DateTimeOffset.Now });

                    var emailMessage = new MimeMessage();
                    emailMessage.From.Add(new MailboxAddress(appConfig.Mail.FromName, appConfig.Mail.FromAddress));
                    emailMessage.To.Add(new MailboxAddress($"{myUser.FirstName} {myUser.LastName}".Trim(), myUser.Email));
                    emailMessage.Subject = Account.EmailTitlePasswordReset;
                    emailMessage.Body = new TextPart("html") {
                        Text = string.Format(Account.EmailTextPasswordReset,
                            urlHelper.Action("ResetPassword", "Account", new { Email = myUser.Email, Hash = hash }, httpContext.Request.Scheme, httpContext.Request.Host.ToUriComponent()))
                    };

                    using (var client = new SmtpClient())
                    {
                        client.Connect(appConfig.Mail.Smtp.Host, appConfig.Mail.Smtp.Port, SecureSocketOptions.None);
                        client.Authenticate(appConfig.Mail.Smtp.Username, appConfig.Mail.Smtp.Password);
                        client.Send(emailMessage);
                        client.Disconnect(true);
                    }

                    return true;
                }
                else
                {
                    error = Account.ErrorInvalidUser;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, ex.Message);
                error = Account.ErrorSendingEmail;
            }
            return false;
        }
    }
}
