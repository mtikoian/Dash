using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dash.Resources;
using Jil;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class Alert : BaseModel, IValidatableObject
    {
        private User _Owner;

        private Report _Report;

        public Alert()
        {
        }

        public Alert(IDbContext dbContext, int userId)
        {
            DbContext = dbContext;
            OwnerId = userId;
        }

        [Ignore, JilDirective(true)]
        public string Cron
        {
            get
            {
                return $"{CronMinute} {CronHour} {CronDayOfMonth} {CronMonth} {CronDayOfWeek}";
            }
        }

        [Display(Name = "CronDayOfMonth", ResourceType = typeof(Alerts))]
        [Required]
        public string CronDayOfMonth { get; set; } = "*";

        [Display(Name = "CronDayOfWeek", ResourceType = typeof(Alerts))]
        [Required]
        public string CronDayOfWeek { get; set; } = "*";

        [Display(Name = "CronHour", ResourceType = typeof(Alerts))]
        [Required]
        public string CronHour { get; set; } = "*";

        [Display(Name = "CronMinute", ResourceType = typeof(Alerts))]
        [Required]
        public string CronMinute { get; set; } = "*";

        [Display(Name = "CronMonth", ResourceType = typeof(Alerts))]
        [Required]
        public string CronMonth { get; set; } = "*";

        [JilDirective(true)]
        public string Hash
        {
            get
            {
                using (var md5 = MD5.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(JSON.Serialize(this)))).Replace("-", "");
                }
            }
        }

        [Display(Name = "IsActive", ResourceType = typeof(Alerts))]
        public bool IsActive { get; set; }

        [Ignore, JilDirective(true)]
        public bool IsOwner { get { return RequestUserId == OwnerId; } }

        [Display(Name = "LastNotificationDate", ResourceType = typeof(Alerts))]
        public DateTimeOffset? LastNotificationDate { get; set; }

        [Display(Name = "LastRunDate", ResourceType = typeof(Alerts))]
        public DateTimeOffset? LastRunDate { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Alerts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "NotificationInterval", ResourceType = typeof(Alerts))]
        [Required]
        public int NotificationInterval { get; set; }

        [Ignore, JilDirective(true)]
        public User Owner { get { return _Owner ?? (_Owner = DbContext.Get<User>(OwnerId)); } }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int OwnerId { get; set; }

        [Ignore, JilDirective(true), BindNever, ValidateNever]
        public Report Report { get { return _Report ?? (_Report = DbContext.Get<Report>(ReportId)); } }

        [Display(Name = "Report", ResourceType = typeof(Alerts))]
        [Required]
        public int ReportId { get; set; }

        [Display(Name = "ResultCount", ResourceType = typeof(Alerts))]
        [Required]
        public int ResultCount { get; set; }

        [Display(Name = "SendTo", ResourceType = typeof(Alerts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(1000, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string SendTo { get; set; }

        [Display(Name = "Subject", ResourceType = typeof(Alerts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Subject { get; set; }

        public Alert Copy(string name = null)
        {
            var newAlert = this.Clone();
            newAlert.Id = 0;
            newAlert.OwnerId = RequestUserId ?? 0;
            newAlert.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;
            return newAlert;
        }

        public IEnumerable<Report> GetReportsForUser(int userId)
        {
            return DbContext.GetAll<Report>(new { UserId = userId }).OrderBy(x => x.Name);
        }

        public bool Save()
        {
            DbContext.Save(this);
            return true;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CronMinute != "*" && !CronMinute.Contains(","))
            {
                var x = CronMinute.ToInt();
                if (x < 0 || x > 59)
                {
                    yield return new ValidationResult(Alerts.ErrorCronMinute, new[] { "CronMinute" });
                }
            }
            if (CronHour != "*" && !CronHour.Contains(","))
            {
                var x = CronHour.ToInt();
                if (x < 0 || x > 23)
                {
                    yield return new ValidationResult(Alerts.ErrorCronHour, new[] { "CronHour" });
                }
            }
            if (CronDayOfMonth != "*" && !CronDayOfMonth.Contains(","))
            {
                var x = CronDayOfMonth.ToInt();
                if (x < 1 || x > 31)
                {
                    yield return new ValidationResult(Alerts.ErrorCronDayOfMonth, new[] { "CronDayOfMonth" });
                }
            }
            if (CronMonth != "*" && !CronMonth.Contains(","))
            {
                var x = CronMonth.ToInt();
                if (x < 1 || x > 12)
                {
                    yield return new ValidationResult(Alerts.ErrorCronMonth, new[] { "CronMonth" });
                }
            }
            if (CronDayOfWeek != "*" && !CronDayOfWeek.Contains(","))
            {
                var x = CronDayOfWeek.ToInt();
                if (x < 0 || x > 6)
                {
                    yield return new ValidationResult(Alerts.ErrorCronDayOfWeek, new[] { "CronDayOfWeek" });
                }
            }

            ValidationResult parseError = null;
            try
            {
                var scheduler = NCrontab.CrontabSchedule.Parse(Cron);
            }
            catch (Exception ex)
            {
                parseError = new ValidationResult(Alerts.ErrorCronParse);
            }
            if (parseError != null)
            {
                yield return parseError;
            }

            // currently the hash will include name so this won't work as expected. need to modify hash creation to only use alert criteria instead
            var duplicateAlert = DbContext.GetAll<Alert>(new { UserId = OwnerId }).FirstOrDefault(x => x.Hash == Hash);
            if (duplicateAlert != null)
            {
                yield return new ValidationResult(string.Format(Alerts.ErrorHash, duplicateAlert.Name));
            }
        }
    }
}
