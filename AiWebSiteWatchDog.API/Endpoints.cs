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
            app.MapGet("/settings", async (ISettingsService settingsService) =>
            {
                return await settingsService.GetSettingsAsync();
            });

            app.MapPost("/settings", async (ISettingsService settingsService, UserSettings settings) =>
            {
                await settingsService.SaveSettingsAsync(settings);
                return Results.Ok();
            });

            // Add more endpoints for watch tasks, notifications, etc.
        }
    }
}
