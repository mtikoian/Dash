using Dash.I18n;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class LogOn : BaseModel
    {
        private IHttpContextAccessor _HttpContextAccessor;

        public LogOn(IHttpContextAccessor httpContextAccessor)
        {
            _HttpContextAccessor = httpContextAccessor;
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
                var membershipService = new AccountMembershipService();
                if (membershipService.ValidateUser(UserName, Password))
                {
                    var myUser = DbContext.GetAll<User>(new { UID = UserName, IsActive = true }).FirstOrDefault();
                    if (myUser != null && myUser.IsActive)
                    {
                        var formsService = new FormsAuthenticationService();
                        formsService.SignIn(UserName, false);
                        return true;
                    }
                    else
                    {
                        error = Account.ErrorCannotMatchMembership;
                    }
                }
                else
                {
                    error = Account.ErrorCannotValidate;
                }
            }
            catch (Exception ex)
            {
                ex.Log(_HttpContextAccessor);
                error = Account.ErrorCannotValidate;
            }
            return false;
        }
    }
}