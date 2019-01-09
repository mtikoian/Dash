using System;
using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Utils;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dash
{
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type)
        {
            return _serviceProvider.GetService(type);
        }
    }

    public class JobHelper
    {
        private IAppConfiguration _AppConfig;
        private IDbContext _DbContext;

        public JobHelper(IDbContext dbContext, IAppConfiguration appConfig)
        {
            _DbContext = dbContext;
            _AppConfig = appConfig;
        }

        [AutomaticRetry(Attempts = 0)]
        public void ProcessAlert(int alertId)
        {
            var alert = _DbContext.Get<Alert>(alertId);
            alert.LastRunDate = DateTimeOffset.Now;

            var emailHelper = new EmailHelper();
            var recipients = alert.SendTo.Split(',').Where(x => emailHelper.IsValidEmail(x));
            if (!recipients.Any())
            {
                return;
            }

            var reportResult = alert.Report.GetData(_AppConfig, 0, 99999, false);
            if (reportResult.Total > alert.ResultCount)
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_AppConfig.Mail.FromName, _AppConfig.Mail.FromAddress));
                recipients.Each(x => emailMessage.To.Add(new MailboxAddress(x)));
                // @todo better subject?
                emailMessage.Subject = $"Dash: {alert.Name}";

                var builder = new BodyBuilder();
                var export = new ExportData { Report = alert.Report, AppConfig = _AppConfig, FileName = $"{alert.Name} {DateTime.Now.Ticks}" };
                builder.Attachments.Add(export.FormattedFileName, export.Stream());
                // @todo better body
                builder.TextBody = "See attached file.";
                emailMessage.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect(_AppConfig.Mail.Smtp.Host, _AppConfig.Mail.Smtp.Port, SecureSocketOptions.None);
                    client.Authenticate(_AppConfig.Mail.Smtp.Username, _AppConfig.Mail.Smtp.Password);
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }

                alert.LastNotificationDate = DateTimeOffset.Now;
            }

            // update alert run/notification dates
            _DbContext.Save(alert);
        }

        public void ProcessAlerts()
        {
            _DbContext.Query<Alert>("AlertGetActive").Each(alert => {
                var scheduler = NCrontab.CrontabSchedule.Parse(alert.Cron);
                var test = scheduler.GetNextOccurrence(DateTime.Now);
                if (scheduler.GetNextOccurrence(DateTime.Now) < DateTime.Now.AddSeconds(60) && (alert.LastNotificationDate == null || alert.LastNotificationDate < DateTime.Now.AddMinutes(-alert.NotificationInterval)))
                {
                    BackgroundJob.Enqueue<JobHelper>(x => x.ProcessAlert(alert.Id));
                }
            });
        }
    }
}
