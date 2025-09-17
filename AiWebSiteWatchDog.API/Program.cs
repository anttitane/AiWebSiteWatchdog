
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.Interfaces;



Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register services (implementations to be added later)
// builder.Services.AddSingleton<IWatcherService, WatcherService>();
// builder.Services.AddSingleton<INotificationService, NotificationService>();
// builder.Services.AddSingleton<ISettingsService, SettingsService>();


var app = builder.Build();

// Enable Swagger middleware
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal API endpoints (to be implemented)
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

app.Run();
