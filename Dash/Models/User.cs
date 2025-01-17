﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Configuration;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    [ModelMetadataType(typeof(PasswordMetadata))]
    public class User : BaseModel, IValidatableObject
    {
        List<Role> _AllRoles;
        List<UserRole> _UserRole;

        public User() { }

        public User(IDbContext dbContext, IAppConfiguration appConfig)
        {
            DbContext = dbContext;
            AppConfig = appConfig;
        }

        [Display(Name = "AllowSingleFactor", ResourceType = typeof(Users))]
        public bool AllowSingleFactor { get; set; } = false;

        [BindNever, ValidateNever]
        public List<Role> AllRoles => _AllRoles ?? (_AllRoles = DbContext.GetAll<Role>().ToList());

        [DbIgnore]
        public string ConfirmPassword { get; set; }

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
        public string FullName => $"{LastName?.Trim()}, {FirstName?.Trim()}".Trim(new char[] { ' ', ',' });

        [DbIgnore, BindNever, ValidateNever]
        public bool IsLocked => LoginAttempts > AppConfig?.Membership.MaxLoginAttempts;

        [DbIgnore]
        public string LanguageCode { get; set; }

        [Display(Name = "Language", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int LanguageId { get; set; }

        [Display(Name = "LastName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string LastName { get; set; }

        [DbIgnore]
        public int LoginAttempts { get; set; }

        [DbIgnore]
        public string Password { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string PasswordHelpText => PasswordHelper.HelpText(AppConfig);

        [Display(Name = "Roles", ResourceType = typeof(Users))]
        [DbIgnore]
        public List<int> RoleIds { get; set; }

        [Display(Name = "UserName", ResourceType = typeof(Users))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string UserName { get; set; }

        [BindNever, ValidateNever]
        public List<UserRole> UserRole
        {
            get => _UserRole ?? (_UserRole ?? DbContext.GetAll<UserRole>(new { UserId = Id }).ToList());
            set => _UserRole = value;
        }

        public bool CanAccessDataset(int datasetId) => Id == DbContext.Query<int>("UserHasDatasetAccess", new { UserId = Id, DatasetId = datasetId }).FirstOrDefault();

        public bool CanViewChart(Chart chart)
        {
            if (chart.UserCreated == Id)
                return true;
            var res = DbContext.Query<bool>("ChartCheckUserAccess", new { UserId = Id, ChartId = chart.Id });
            return res?.Any() == true && res.First();
        }

        public bool CanViewReport(Report report)
        {
            if (report.UserCreated == Id)
                return true;
            var res = DbContext.Query<bool>("ReportCheckUserAccess", new { UserId = Id, ReportId = report.Id });
            return res?.Any() == true && res.First();
        }

        public bool Save(bool lazySave = true)
        {
            var keyedUserRoles = DbContext.GetAll<UserRole>(new { UserId = Id }).ToDictionary(x => x.RoleId, x => x);
            UserRole = RoleIds?.Where(x => x > 0).Select(id => keyedUserRoles.ContainsKey(id) ? keyedUserRoles[id] : new UserRole { UserId = Id, RoleId = id }).ToList()
                ?? new List<UserRole>();
            UserRole.Each(x => x.RequestUserId = RequestUserId);

            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                if (lazySave)
                    DbContext.SaveMany(this, UserRole);

                if (!Password.IsEmpty())
                {
                    var salt = Hasher.GenerateSalt();
                    DbContext.Execute("UserPasswordSave", new { Id = Id, Password = Hasher.HashPassword(Password, salt), Salt = salt, RequestUserId = RequestUserId });
                }
            });

            return true;
        }

        public void Unlock() => DbContext.Execute("UserLoginAttemptsSave", new { Id = Id, LoginAttempts = 0, DateUnlocks = DateTimeOffset.MinValue });

        public bool UpdateProfile(out string errorMsg)
        {
            // load user object and copy settings the user is allowed to change
            var user = DbContext.Get<User>(Id);
            user.FirstName = FirstName;
            user.LastName = LastName;
            user.LanguageId = LanguageId;
            user.Email = Email;
            user.Password = Password;
            user.ConfirmPassword = ConfirmPassword;

            errorMsg = "";
            var context = new ValidationContext(user, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(user, context, validationResults, true);
            if (!isValid)
            {
                errorMsg = validationResults.ToErrorString();
                return false;
            }

            if (user.Save(false))
            {
                var language = DbContext.Get<Language>(user.LanguageId);
                if (language != null)
                    LanguageCode = language.LanguageCode;
                return true;
            }
            errorMsg = Core.ErrorGeneric;
            return false;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DbContext.GetAll<User>(new { UserName }).Any(x => x.Id != Id))
                yield return new ValidationResult(Users.ErrorDuplicateName, new[] { "UserName" });
            if (Id == 0)
                if (Password.IsEmpty())
                    yield return new ValidationResult(Account.ErrorInvalidPassword, new[] { "Password" });
                else
                    yield return PasswordHelper.Validate(AppConfig, Password, ConfirmPassword);
            else if (!Password.IsEmpty())
                yield return PasswordHelper.Validate(AppConfig, Password, ConfirmPassword);
        }
    }
}
