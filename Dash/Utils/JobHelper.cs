﻿using System;
using System.Linq;
using System.Net.Http;
using Dash.Configuration;
using Dash.Models;
using Dash.Utils;
using Hangfire;
using Jil;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dash
{
    public class HangfireActivator : JobActivator
    {
        readonly IServiceProvider _ServiceProvider;

        public HangfireActivator(IServiceProvider serviceProvider) => _ServiceProvider = serviceProvider;

        public override object ActivateJob(Type type) => _ServiceProvider.GetService(type);
    }

    public class JobHelper
    {
        IAppConfiguration _AppConfig;
        IHttpClientFactory _ClientFactory;
        IDbContext _DbContext;

        public JobHelper(IDbContext dbContext, IAppConfiguration appConfig, IHttpClientFactory clientFactory)
        {
            _DbContext = dbContext;
            _AppConfig = appConfig;
            _ClientFactory = clientFactory;
        }

        [AutomaticRetry(Attempts = 0)]
        public void ProcessAlert(int alertId)
        {
            var alert = _DbContext.Get<Alert>(alertId);
            alert.LastRunDate = DateTimeOffset.Now;

            var emailHelper = new EmailHelper();
            var recipients = alert.SendToEmail?.Split(',').Where(x => emailHelper.IsValidEmail(x));

            if (recipients?.Any() != true && alert.SendToWebhook.IsEmpty())
                return;

            var reportResult = alert.Report.GetData(_AppConfig, 0, 99999, false);
            if (reportResult.Total > alert.ResultCount)
            {
                var subject = $"Dash Alert: {alert.Name} ({reportResult.Total} records)";
                if (recipients?.Any() == true)
                {
                    var emailMessage = new MimeMessage();
                    emailMessage.From.Add(new MailboxAddress(_AppConfig.Mail.FromName, _AppConfig.Mail.FromAddress));
                    recipients.Each(x => emailMessage.To.Add(new MailboxAddress(x)));
                    emailMessage.Subject = subject;

                    var export = new ExportData { Report = alert.Report, AppConfig = _AppConfig, FileName = $"{alert.Name} {DateTime.Now.Ticks}" };
                    var builder = new BodyBuilder { TextBody = "See attached file." };
                    builder.Attachments.Add(export.FormattedFileName, export.Stream());
                    emailMessage.Body = builder.ToMessageBody();

                    // @todo add in Polly circuitbreaker
                    using (var client = new SmtpClient())
                    {
                        client.ConnectAsync(_AppConfig.Mail.Smtp.Host, _AppConfig.Mail.Smtp.Port, SecureSocketOptions.None);
                        if (!_AppConfig.Mail.Smtp.Username.IsEmpty())
                            client.AuthenticateAsync(_AppConfig.Mail.Smtp.Username, _AppConfig.Mail.Smtp.Password);
                        client.SendAsync(emailMessage);
                        client.DisconnectAsync(true);
                    }
                }

                if (!alert.SendToWebhook.IsEmpty())
                {
                    // named client will handle circuit breaker logic
                    var res = _ClientFactory.CreateClient(Startup.TeamsClient).PostAsync(alert.SendToWebhook, new StringContent(JSON.Serialize(new { Text = subject }))).Result;
                    if (!res.IsSuccessStatusCode)
                        throw new Exception($"Error sending to webhook: {res.StatusCode}: {res.Content.ReadAsStringAsync().Result}");
                }

                alert.LastNotificationDate = DateTimeOffset.Now;
            }

            // update alert run/notification dates
            _DbContext.Save(alert);
        }

        public void ProcessAlerts()
        {
            _DbContext.Query<Alert>("AlertGetActive").Each(alert => {
                if (NCrontab.CrontabSchedule.Parse(alert.Cron).GetNextOccurrence(DateTime.Now) < DateTime.Now.AddSeconds(60) && (alert.LastNotificationDate == null || alert.LastNotificationDate < DateTime.Now.AddMinutes(-alert.NotificationInterval)))
                    BackgroundJob.Enqueue<JobHelper>(x => x.ProcessAlert(alert.Id));
            });
        }
    }
}
