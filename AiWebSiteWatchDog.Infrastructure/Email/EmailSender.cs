using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

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

            using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(emailSettings.SenderEmail, emailSettings.AppPassword),
                EnableSsl = emailSettings.EnableSsl
            };
            await client.SendMailAsync(message);
        }
    }
}
