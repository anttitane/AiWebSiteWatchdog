using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;

namespace AiWebSiteWatchDog.API
{
    public static class Endpoints
    {
        public static void MapApiEndpoints(this WebApplication app)
        {
            // User settings endpoints
            app.MapGet("/settings", async (ISettingsService settingsService) =>
            {
                return await settingsService.GetSettingsAsync();
            });

            app.MapPut("/settings", async (ISettingsService settingsService, UserSettings settings) =>
            {
                await settingsService.SaveSettingsAsync(settings);
                return Results.Ok();
            });

            // Email settings endpoints
            app.MapGet("/email-settings/{senderEmail}", async (AiWebSiteWatchDog.Infrastructure.Persistence.EmailSettingsRepository repo, string senderEmail) =>
            {
                var settings = await repo.GetAsync(senderEmail);
                return settings is not null ? Results.Ok(settings) : Results.NotFound();
            });

            app.MapPut("/email-settings", async (AiWebSiteWatchDog.Infrastructure.Persistence.EmailSettingsRepository repo, EmailSettings settings) =>
            {
                await repo.SaveAsync(settings);
                return Results.Ok();
            });

            // Watch tasks endpoints
            app.MapGet("/tasks", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo) =>
            {
                return await repo.GetAllAsync();
            });

            app.MapPost("/tasks", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, WatchTask task) =>
            {
                await repo.AddAsync(task);
                return Results.Ok();
            });

            app.MapGet("/tasks/{id}", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id) =>
            {
                var task = await repo.GetByIdAsync(id);
                return task is not null ? Results.Ok(task) : Results.NotFound();
            });

            app.MapPut("/tasks/{id}", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id, WatchTask updated) =>
            {
                var result = await repo.UpdateAsync(id, updated);
                return result ? Results.Ok() : Results.NotFound();
            });

            app.MapDelete("/tasks/{id}", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id) =>
            {
                var result = await repo.DeleteAsync(id);
                return result ? Results.Ok() : Results.NotFound();
            });

            // Manual trigger endpoint
            app.MapPost("/tasks/{id}/run", async (AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, IWatcherService watcherService, int id) =>
            {
                var task = await repo.GetByIdAsync(id);
                if (task is null) return Results.NotFound();
                // Provide dummy/default values for required UserSettings constructor parameters
                var settings = new UserSettings(
                    email: "dummy@example.com",
                    geminiApiKey: string.Empty,
                    watchUrl: task.Url,
                    interestSentence: task.InterestSentence,
                    schedule: string.Empty,
                    emailSettingsSenderEmail: "dummy@example.com"
                );
                var result = await watcherService.CheckWebsiteAsync(settings);
                return Results.Ok(result);
            });

            // Notifications endpoints
            app.MapGet("/notifications", async (AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository repo) =>
            {
                return await repo.GetAllAsync();
            });

            app.MapGet("/notifications/{id}", async (AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository repo, int id) =>
            {
                var notification = await repo.GetByIdAsync(id);
                return notification is not null ? Results.Ok(notification) : Results.NotFound();
            });

            app.MapDelete("/notifications/{id}", async (AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository repo, int id) =>
            {
                var result = await repo.DeleteAsync(id);
                return result ? Results.Ok() : Results.NotFound();
            });

            // Health/status endpoint
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        }
    }
}
