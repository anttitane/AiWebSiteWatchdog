using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Email
{
    public class EmailSender
    {
        public async Task SendAsync(Notification notification, EmailSettings emailSettings)
        {
            var message = new MailMessage();
            message.From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName);
            message.To.Add(notification.Email);
            message.Subject = notification.Subject;
            message.Body = notification.Message;

            try
            {
                using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(emailSettings.SenderEmail, emailSettings.AppPassword),
                    EnableSsl = emailSettings.EnableSsl
                };
                Log.Information("Sending email to {Email} via {SmtpServer}:{SmtpPort}", notification.Email, emailSettings.SmtpServer, emailSettings.SmtpPort);
                await client.SendMailAsync(message);
                Log.Information("Email sent to {Email} with subject '{Subject}'", notification.Email, notification.Subject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send email to {Email}", notification.Email);
                throw;
            }
        }
    }
}
