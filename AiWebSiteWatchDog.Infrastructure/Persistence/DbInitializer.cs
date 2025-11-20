using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void EnsureMigrated(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Desired behavior:
            // - Create schema when the app's tables do not exist yet (initial run)
            // - If the schema already exists, do not apply migrations
            try
            {
                // Always apply pending migrations to keep schema current across versions
                Log.Information("Applying EF Core migrations (if any) to ensure schema is up-to-date.");
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed during database initialization.");
                throw;
            }
        }
    }
}
