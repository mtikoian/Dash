using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class UpdateAccount : BaseModel
    {
        public UpdateAccount()
        {
        }

        public UpdateAccount(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        public UpdateAccount(IDbContext dbContext, IAppConfiguration appConfig, string userName)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
            var user = DbContext.GetAll<User>(new { UserName = userName }).First();
            Email = user.Email;
            FirstName = user.FirstName;
            LastName = user.LastName;
            LanguageId = user.LanguageId;
        }

        [Display(Name = "Email", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [EmailAddress(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorEmailAddress")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorEmailAddressFormat")]
        public string Email { get; set; }

        [Display(Name = "FirstName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string FirstName { get; set; }

        [DbIgnore]
        public string LanguageCode { get; set; }

        [Display(Name = "Language", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int LanguageId { get; set; }

        [Display(Name = "LastName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string LastName { get; set; }

        public IEnumerable<SelectListItem> GetLanguageList()
        {
            return DbContext.GetAll<Language>().ToSelectList(x => x.Name, x => x.Id.ToString());
        }

        public bool Save(string userName, out string errorMsg)
        {
            errorMsg = "";
            // load user object and copy settings the user is allowed to change
            if (!RequestUserId.HasPositiveValue())
            {
                errorMsg = Account.ErrorGeneric;
                return false;
            }
            var user = DbContext.Get<User>(RequestUserId.Value);
            if (user == null)
            {
                errorMsg = Account.ErrorGeneric;
                return false;
            }
            user.FirstName = FirstName;
            user.LastName = LastName;
            user.LanguageId = LanguageId;
            user.Email = Email;

            if (user.Save(false))
            {
                var language = DbContext.Get<Language>(user.LanguageId);
                if (language != null)
                {
                    LanguageCode = language.LanguageCode;
                }
                return true;
            }
            errorMsg = Account.ErrorGeneric;
            return false;
        }
    }
}
