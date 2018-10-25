﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public class ReportShare : BaseModel, IValidatableObject
    {
        private Report _Report;

        public ReportShare()
        {
        }

        public ReportShare(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ReportShare(IDbContext dbContext, int reportId)
        {
            DbContext = dbContext;
            ReportId = reportId;
        }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ReportId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ReportName { get { return Report?.Name; } }

        [Display(Name = "Role", ResourceType = typeof(Core))]
        public int? RoleId { get; set; }

        [DbIgnore]
        public string RoleName { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> RoleSelectListItems
        {
            get
            {
                return DbContext.GetAll<Role>().OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Id.ToString());
            }
        }

        [Display(Name = "User", ResourceType = typeof(Core))]
        public int? UserId { get; set; }

        [DbIgnore]
        public string UserName { get { return $"{UserLastName?.Trim()}, {UserFirstName?.Trim()}".Trim(new char[] { ' ', ',' }); } }

        [DbIgnore]
        public string UserFirstName { get; set; }

        [DbIgnore]
        public string UserLastName { get; set; }

        [BindNever, ValidateNever]
        public IEnumerable<SelectListItem> UserSelectListItems
        {
            get
            {
                return DbContext.GetAll<User>().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToSelectList(x => x.FullName, x => x.Id.ToString());
            }
        }

        private Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(ReportId)); } }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!UserId.HasValue && !RoleId.HasValue)
            {
                yield return new ValidationResult(Core.ErrorUserOrRole, new[] { "UserID" });
            }
        }
    }
}
