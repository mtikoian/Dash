﻿using System;
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
        Report _Report;

        public Alert() { }

        public Alert(IDbContext dbContext) => DbContext = dbContext;

        [DbIgnore, JilDirective(true)]
        public string Cron => $"{CronMinute} {CronHour} {CronDayOfMonth} {CronMonth} {CronDayOfWeek}";

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
                    return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(JSON.Serialize(this)))).Replace("-", "");
            }
        }

        [Display(Name = "IsActive", ResourceType = typeof(Alerts))]
        public bool IsActive { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public bool IsOwner
        {
            get
            {
                if (UserCreated == 0 && Id > 0)
                    UserCreated = DbContext.Get<Alert>(Id)?.UserCreated ?? 0;
                return RequestUserId == UserCreated;
            }
        }

        [Display(Name = "LastNotificationDate", ResourceType = typeof(Alerts))]
        public DateTimeOffset? LastNotificationDate { get; set; }

        [Display(Name = "LastRunDate", ResourceType = typeof(Alerts))]
        public DateTimeOffset? LastRunDate { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Alerts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string Name { get; set; }

        [Display(Name = "NotificationInterval", ResourceType = typeof(Alerts))]
        [Required]
        public int NotificationInterval { get; set; }

        [DbIgnore, JilDirective(true), BindNever, ValidateNever]
        public Report Report => _Report ?? (_Report = DbContext.Get<Report>(ReportId));

        [Display(Name = "Report", ResourceType = typeof(Alerts))]
        [Required]
        public int ReportId { get; set; }

        [Display(Name = "ResultCount", ResourceType = typeof(Alerts))]
        [Required]
        public int ResultCount { get; set; }

        [Display(Name = "SendToEmail", ResourceType = typeof(Alerts))]
        [StringLength(1000, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string SendToEmail { get; set; }

        [Display(Name = "SendToWebhook", ResourceType = typeof(Alerts))]
        [StringLength(1000, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string SendToWebhook { get; set; }

        [Display(Name = "Subject", ResourceType = typeof(Alerts))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Subject { get; set; }

        [JilDirective(true), DbIgnore]
        public int UserCreated { get; set; }

        public Alert Copy(string name = null)
        {
            var newAlert = this.Clone();
            newAlert.Id = 0;
            newAlert.Name = name.IsEmpty() ? string.Format(Core.CopyOf, Name) : name;
            return newAlert;
        }

        public IEnumerable<Report> GetReportsForUser(int userId) => DbContext.GetAll<Report>(new { UserId = userId }).OrderBy(x => x.Name);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SendToEmail.IsEmpty() && SendToWebhook.IsEmpty())
                yield return new ValidationResult(Alerts.ErrorEmailOrWebhookRequired, new[] { "SendToEmail" });
            if (CronMinute != "*" && !CronMinute.Contains(","))
            {
                var x = CronMinute.ToInt();
                if (x < 0 || x > 59)
                    yield return new ValidationResult(Alerts.ErrorCronMinute, new[] { "CronMinute" });
            }
            if (CronHour != "*" && !CronHour.Contains(","))
            {
                var x = CronHour.ToInt();
                if (x < 0 || x > 23)
                    yield return new ValidationResult(Alerts.ErrorCronHour, new[] { "CronHour" });
            }
            if (CronDayOfMonth != "*" && !CronDayOfMonth.Contains(","))
            {
                var x = CronDayOfMonth.ToInt();
                if (x < 1 || x > 31)
                    yield return new ValidationResult(Alerts.ErrorCronDayOfMonth, new[] { "CronDayOfMonth" });
            }
            if (CronMonth != "*" && !CronMonth.Contains(","))
            {
                var x = CronMonth.ToInt();
                if (x < 1 || x > 12)
                    yield return new ValidationResult(Alerts.ErrorCronMonth, new[] { "CronMonth" });
            }
            if (CronDayOfWeek != "*" && !CronDayOfWeek.Contains(","))
            {
                var x = CronDayOfWeek.ToInt();
                if (x < 0 || x > 6)
                    yield return new ValidationResult(Alerts.ErrorCronDayOfWeek, new[] { "CronDayOfWeek" });
            }

            ValidationResult parseError = null;
            try
            {
                var scheduler = NCrontab.CrontabSchedule.Parse(Cron);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Cron Exception");
                parseError = new ValidationResult(Alerts.ErrorCronParse);
            }
            if (parseError != null)
                yield return parseError;

            var duplicateAlert = DbContext.GetAll<Alert>(new { UserId = RequestUserId ?? UserCreated }).FirstOrDefault(x => x.Hash == Hash);
            if (duplicateAlert != null && duplicateAlert.Id != Id)
                yield return new ValidationResult(string.Format(Alerts.ErrorHash, duplicateAlert.Name));
        }
    }
}
