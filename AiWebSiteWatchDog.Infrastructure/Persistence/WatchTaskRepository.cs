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

        public async Task<WatchTask?> GetByIdAsync(int id)
        {
            return await _dbContext.WatchTasks.FindAsync(id);
        }

        public async Task AddAsync(WatchTask task)
        {
            await _dbContext.WatchTasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();
            Log.Information("WatchTask saved to database.");
        }

        public async Task<bool> UpdateAsync(int id, WatchTask updated)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;
            _dbContext.Entry(existing).CurrentValues.SetValues(updated);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.WatchTasks.FindAsync(id);
            if (existing == null) return false;
            _dbContext.WatchTasks.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
