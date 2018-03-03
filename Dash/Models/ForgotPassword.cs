using FluentEmail;
using Dash.I18n;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dash.Models
{
    public class ForgotPassword : BaseModel
    {
        private IActionContextAccessor ActionContextAccessor;
        private IHttpContextAccessor HttpContextAccessor;

        public ForgotPassword(IHttpContextAccessor httpContextAccessor, IActionContextAccessor actionContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
            ActionContextAccessor = actionContextAccessor;
        }

        [Display(Name = "UID", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [MaxLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string UserName { get; set; }

        /// <summary>
        /// Send an email with a password reset link.
        /// </summary>
        /// <param name="error">Error message if any.</param>
        /// <returns>True on success, else false.</returns>
        public bool Send(out string error)
        {
            error = "";
            try
            {
                var myUser = DbContext.GetAll<User>(new { UID = UserName }).FirstOrDefault();
                if (myUser != null)
                {
                    var hash = Guid.NewGuid().ToString();
                    DbContext.Execute("UserResetSave", new { Id = myUser.Id, ResetHash = hash, DateReset = DateTimeOffset.Now });

                    // email reset link to user
                    var helper = new UrlHelper(ActionContextAccessor.ActionContext);
                    var email = Email.FromDefault()
                        .To(myUser.Email)
                        .Subject(Account.EmailTitlePasswordReset)
                        .UsingTemplate(Account.EmailTextPasswordReset, new { Url = helper.Action("ResetPassword", "Account", new { Email = myUser.Email, Hash = hash },
                            ActionContextAccessor.ActionContext.HttpContext.Request.Scheme) });
                    email.Send();

                    return true;
                }
                else
                {
                    error = Account.ErrorInvalidUser;
                }
            }
            catch (Exception ex)
            {
                ex.Log(HttpContextAccessor);
                error = Account.ErrorSendingEmail;
            }
            return false;
        }
    }
}