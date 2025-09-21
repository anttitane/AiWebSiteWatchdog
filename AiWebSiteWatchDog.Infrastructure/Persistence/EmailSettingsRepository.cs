using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class EmailSettingsRepository(AppDbContext _dbContext)
    {
        public async Task<EmailSettings?> GetAsync(string senderEmail)
        {
            return await _dbContext.EmailSettings.FirstOrDefaultAsync(e => e.SenderEmail == senderEmail);
        }

        public async Task SaveAsync(EmailSettings settings)
        {
            var existing = await _dbContext.EmailSettings.FirstOrDefaultAsync(e => e.SenderEmail == settings.SenderEmail);
            if (existing == null)
            {
                await _dbContext.EmailSettings.AddAsync(settings);
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(settings);
            }
            await _dbContext.SaveChangesAsync();
            Log.Information("EmailSettings saved to database.");
        }
    }
}
