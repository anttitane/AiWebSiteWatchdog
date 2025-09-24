using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;
using System.IO;
using System.Threading;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Email
{
    public class EmailSender() : IEmailSender
    {
        public async Task SendAsync(Notification notification, EmailSettings emailSettings, string recipientEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailSettings.GmailClientSecretJson))
                    throw new ArgumentException("GmailClientSecretJson must be provided for Gmail API OAuth2.");

                using var secretStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emailSettings.GmailClientSecretJson));
                var googleSecrets = GoogleClientSecrets.FromStream(secretStream).Secrets;

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    googleSecrets,
                    [GmailService.Scope.GmailSend],
                    emailSettings.SenderEmail,
                    CancellationToken.None,
                    new FileDataStore("GmailApiToken")
                );

                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AiWebSiteWatchdog"
                });

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));
                emailMessage.To.Add(new MailboxAddress("Recipient", recipientEmail));
                emailMessage.Subject = notification.Subject;
                emailMessage.Body = new TextPart("plain") { Text = notification.Message };

                using var stream = new MemoryStream();
                emailMessage.WriteTo(stream);
                var rawMessage = Convert.ToBase64String(stream.ToArray())
                    .Replace('+', '-').Replace('/', '_').Replace("=", "");

                var message = new Message { Raw = rawMessage };
                Log.Information("Sending email to {Email} via Gmail API", recipientEmail);
                await service.Users.Messages.Send(message, "me").ExecuteAsync();
                Log.Information("Email sent to {Email} with subject '{Subject}'", recipientEmail, notification.Subject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception in EmailSender.SendAsync for recipient {Email}", recipientEmail);
                throw;
            }
        }
    }
}
