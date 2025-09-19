using System.Collections.Generic;
using System.Threading.Tasks;
using AiWebSiteWatchDog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class WatchTaskRepository
    {
        private readonly AppDbContext _dbContext;

        public WatchTaskRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<WatchTask>> GetAllAsync()
        {
            return await _dbContext.WatchTasks.ToListAsync();
        }

        public async Task AddAsync(WatchTask task)
        {
            await _dbContext.WatchTasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();
            Log.Information("WatchTask saved to database.");
        }
    }
}
