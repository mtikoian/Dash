﻿using Dash.I18n;
using Dash.Utils;
using Jil;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// User is a single application user. Used for authentication/authorization.
    /// </summary>
    [HasMany(typeof(UserRole))]
    public class User : BaseModel, IValidatableObject
    {
        private List<Role> _AllRoles;
        private List<UserRole> _UserRole;

        /// <summary>
        /// Make a keyed list of all roles.
        /// </summary>
        /// <returns>Dictionary of roles keyed by roleID.</returns>
        [JilDirective(true)]
        public List<Role> AllRoles { get { return _AllRoles ?? (_AllRoles = DbContext.GetAll<Role>().ToList()); } }

        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(I18n.Users))]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Ignore, JilDirective(true)]
        public string ConfirmPassword { get; set; }

        [Ignore, JilDirective(true)]
        public DateTimeOffset? DateReset { get; set; }

        [Display(Name = "Email", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [EmailAddress(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorEmailAddress")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorEmailAddressFormat")]
        public string Email { get; set; }

        [Ignore, JilDirective(true)]
        public string Error { get; set; }

        [Display(Name = "FirstName", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string FirstName { get; set; }

        /// <summary>
        /// Get the full name of the user.
        /// </summary>
        /// <returns>Returns name as "First Last".</returns>
        [Ignore, JilDirective(true)]
        public string FullName
        {
            get { return $"{FirstName.Trim()} {LastName}".Trim(); }
        }

        [Display(Name = "IsActive", ResourceType = typeof(I18n.Users))]
        [JilDirective(true)]
        public bool IsActive { get; set; } = false;

        [Ignore, JilDirective(true)]
        public string LanguageCode { get; set; }

        [Display(Name = "Language", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int LanguageId { get; set; }

        [Display(Name = "LastName", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string LastName { get; set; }

        [StringLength(250, MinimumLength = 6, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMinMaxLength")]
        [Display(Name = "Password", ResourceType = typeof(I18n.Users))]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Ignore, JilDirective(true)]
        public string Password { get; set; }

        [Ignore, JilDirective(true)]
        public string ResetHash { get; set; }

        [Ignore]
        public List<int> RoleIds { get; set; }

        [Ignore, JilDirective(true)]
        public string Salt { get; set; }

        [Display(Name = "UID", ResourceType = typeof(I18n.Users))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(Name = "UID")]
        public string UID { get; set; }

        [JilDirective(true)]
        public List<UserRole> UserRole
        {
            get { return _UserRole ?? (_UserRole ?? DbContext.GetAll<UserRole>(new { UserId = Id }).ToList()); }
            set { _UserRole = value; }
        }

        /// <summary>
        /// Check the user has permissions to access the requested dataset.
        /// </summary>
        /// <param name="dataSet">DatasetId to check permissions for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool CanAccessDataset(int datasetId)
        {
            return Id == DbContext.Query<int>("UserHasDatasetAccess", new { UserId = Id, DatasetId = datasetId }).FirstOrDefault();
        }

        /// <summary>
        /// Check if the user can view a chart.
        /// </summary>
        /// <param name="report">Chart to check access for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool CanViewChart(Chart chart)
        {
            if (chart.OwnerId == Id)
            {
                return true;
            }
            var res = DbContext.Query<bool>("ChartCheckUserAccess", new { UserId = Id, ChartId = chart.Id });
            return res?.Any() == true && res.First();
        }

        /// <summary>
        /// Check if a user can view a report.
        /// </summary>
        /// <param name="report">Report to check access for.</param>
        /// <returns>Returns true if the user has access, else false.</returns>
        public bool CanViewReport(Report report)
        {
            if (report.OwnerId == Id)
            {
                return true;
            }
            var res = DbContext.Query<bool>("ReportCheckUserAccess", new { UserId = Id, ReportId = report.Id });
            return res?.Any() == true && res.First();
        }

        /// <summary>
        /// Save a user, including updating the userRoles and password if provided.
        /// </summary>
        /// <param name="lazySave">Lazy save children if true.</param>
        /// <returns>True if successful, else false.</returns>
        public bool Save(bool lazySave = true)
        {
            // set the new roles
            if (RoleIds?.Any() == true)
            {
                // make a list of all user roles
                var keyedUserRoles = DbContext.GetAll<UserRole>(new { UserId = Id }).ToDictionary(x => x.RoleId, x => x);
                UserRole = RoleIds?.Where(x => x > 0).Select(id => keyedUserRoles.ContainsKey(id) ? keyedUserRoles[id] : new UserRole { UserId = Id, RoleId = id }).ToList()
                    ?? new List<UserRole>();
            }

            // try saving
            DbContext.Save(this);

            if (!Password.IsEmpty())
            {
                Salt = Hasher.GenerateSalt();
                DbContext.Execute("UserPasswordSave", new { Id = Id, Password = Hasher.HashPassword(Password, Salt), Salt = Salt, RequestUserId = RequestUserId });
            }

            return true;
        }

        /// <summary>
        /// Update a user object using values from the model. Used by the user when updating their own profile.
        /// </summary>
        /// <param name="errorMsg">Error message from validation if any.</param>
        /// <returns>True if update successful, else false.</returns>
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
                {
                    var cultureInfo = new CultureInfo(language.LanguageCode);
                    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                }
                return true;
            }
            errorMsg = Account.ErrorGeneric;
            return false;
        }

        /// <summary>
        /// Validate user object. Check that UID is unique and password meets requirements.
        /// Don't make this automatic using IValidatableObject - the membershipProvider authorization will run it on every single request.
        /// </summary>
        /// <returns>Returns a list of errors if any.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            // check for unique UID
            if (DbContext.GetAll<User>(new { UID }).Any(x => x.Id != Id))
            {
                yield return new ValidationResult(I18n.Users.ErrorDuplicateName, new[] { "UID" });
            }

            if (Id == 0)
            {
                if (Password.IsEmpty())
                {
                    yield return new ValidationResult(I18n.Account.ErrorInvalidPassword, new[] { "Password" });
                }
                else if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(I18n.Account.ErrorPasswordMatch, new[] { "ConfirmPassword" });
                }
                else if ((Password.Length < AppConfig.Membership.MinRequiredPasswordLength) || Password.ToCharArray().Count(c => !Char.IsLetterOrDigit(c)) < AppConfig.Membership.MinRequiredNonAlphanumericCharacters)
                {
                    yield return new ValidationResult(I18n.Account.ErrorInvalidPassword, new[] { "Password" });
                }
            }
            else if (!Password.IsEmpty())
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(I18n.Account.ErrorPasswordMatch, new[] { "ConfirmPassword" });
                }
                else if ((Password.Length < AppConfig.Membership.MinRequiredPasswordLength) || Password.ToCharArray().Count(c => !Char.IsLetterOrDigit(c)) < AppConfig.Membership.MinRequiredNonAlphanumericCharacters)
                {
                    yield return new ValidationResult(I18n.Account.ErrorInvalidPassword, new[] { "Password" });
                }
            }
        }
    }
}