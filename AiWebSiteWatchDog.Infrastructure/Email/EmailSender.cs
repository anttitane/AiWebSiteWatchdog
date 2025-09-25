using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using System.IO;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;
using Serilog;
using AiWebSiteWatchDog.Infrastructure.Auth;

namespace AiWebSiteWatchDog.Infrastructure.Email
{
    public class EmailSender(IGoogleCredentialProvider credentialProvider) : IEmailSender
    {
        private readonly IGoogleCredentialProvider _credentialProvider = credentialProvider;
        public async Task SendAsync(Notification notification, EmailSettings emailSettings, string recipientEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailSettings.GmailClientSecretJson))
                    throw new ArgumentException("GmailClientSecretJson must be provided for Gmail API OAuth2.");

                var credential = await _credentialProvider.GetGmailAndGeminiCredentialAsync(
                    emailSettings.SenderEmail,
                    emailSettings.GmailClientSecretJson
                );

                var service = new GmailService(new BaseClientService.Initializer
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
