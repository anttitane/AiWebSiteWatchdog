using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // Use the same connection string as in production. Prefer an environment
            // variable (used by EF tools) and fall back to a sensible default.
            var envConn = System.Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            var connectionString = string.IsNullOrWhiteSpace(envConn)
                ? "Data Source=AiWebSiteWatchdog.db"
                : envConn;
            optionsBuilder.UseSqlite(connectionString);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
