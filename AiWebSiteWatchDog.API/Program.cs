
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using AiWebSiteWatchDog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register EF Core DbContext
builder.Services.AddDbContext<AiWebSiteWatchDog.Infrastructure.Persistence.AppDbContext>(options =>
    options.UseSqlite("Data Source=AiWebSiteWatchdog.db"));

// Register infrastructure implementations
builder.Services.AddScoped<ISettingsRepository, AiWebSiteWatchDog.Infrastructure.Persistence.SQLiteSettingsRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.EmailSettingsRepository>();
builder.Services.AddSingleton<IEmailSender, AiWebSiteWatchDog.Infrastructure.Email.EmailSender>();
builder.Services.AddSingleton<IGeminiApiClient, AiWebSiteWatchDog.Infrastructure.Gemini.GeminiApiClient>();

// Register application services
builder.Services.AddSingleton<ISettingsService, AiWebSiteWatchDog.Application.Services.SettingsService>();
builder.Services.AddSingleton<INotificationService, AiWebSiteWatchDog.Application.Services.NotificationService>();
builder.Services.AddSingleton<IWatcherService, AiWebSiteWatchDog.Application.Services.WatcherService>();

var app = builder.Build();

// Ensure database is migrated and tables are created
AiWebSiteWatchDog.Infrastructure.Persistence.DbInitializer.EnsureMigrated(app.Services);

// Enable Swagger middleware
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Register API endpoints from separate file
AiWebSiteWatchDog.API.Endpoints.MapApiEndpoints(app);

app.Run();
