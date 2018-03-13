using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.I18n;
using FluentEmail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dash.Models
{
    public class ForgotPassword : BaseModel
    {
        private ActionContext _ActionContext;

        public ForgotPassword(IActionContextAccessor actionContextAccessor)
        {
            _ActionContext = actionContextAccessor.ActionContext;
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
                    var helper = new UrlHelper(_ActionContext);
                    var email = Email.FromDefault()
                        .To(myUser.Email)
                        .Subject(Account.EmailTitlePasswordReset)
                        .UsingTemplate(Account.EmailTextPasswordReset, new {
                            Url = helper.Action("ResetPassword", "Account", new { myUser.Email, hash },
                            _ActionContext.HttpContext.Request.Scheme)
                        });
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
                ex.Log();
                error = Account.ErrorSendingEmail;
            }
            return false;
        }
    }
}