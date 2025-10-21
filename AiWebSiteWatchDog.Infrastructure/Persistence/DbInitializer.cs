using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Serilog;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void EnsureMigrated(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = db.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
            {
                Log.Information("Applying {Count} pending EF Core migrations: {Migrations}", pending.Count, string.Join(", ", pending));
            }
            else
            {
                Log.Information("No pending EF Core migrations detected.");
            }

            db.Database.Migrate();

            var applied = db.Database.GetAppliedMigrations().ToList();
            Log.Information("EF Core migrations applied (total {Count}): {Migrations}", applied.Count, string.Join(", ", applied));
        }
    }
}
