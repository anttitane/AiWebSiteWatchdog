using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Infrastructure.Email
{
    public class EmailSender
    {
        public async Task SendAsync(Notification notification)
        {
            // TODO: Implement email sending
            await Task.Delay(100); // Simulate async
        }
    }
}
