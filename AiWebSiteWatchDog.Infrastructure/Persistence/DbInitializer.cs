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
                if (db.Database.IsSqlite())
                {
                    var conn = db.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    bool HasTable(string name)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name;";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "$name";
                        p.Value = name;
                        cmd.Parameters.Add(p);
                        using var reader = cmd.ExecuteReader();
                        return reader.Read();
                    }

                    // If none of the app tables exist, create schema using migrations (initial create)
                    var hasAppTables = HasTable("UserSettings") || HasTable("WatchTasks") || HasTable("Notifications") || HasTable("GoogleOAuthTokens");
                    if (!hasAppTables)
                    {
                        Log.Information("No application tables found. Applying initial migrations to create schema.");
                        db.Database.Migrate();
                    }
                    else
                    {
                        Log.Information("Application tables already exist. Skipping EF Core migrations.");
                    }
                }
                else
                {
                    // Non-SQLite providers: create schema only if the database is not reachable
                    if (!db.Database.CanConnect())
                    {
                        Log.Information("Database not reachable. Applying initial migrations to create schema.");
                        db.Database.Migrate();
                    }
                    else
                    {
                        Log.Information("Database reachable. Skipping EF Core migrations.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed during database initialization.");
                throw;
            }
        }
    }
}
