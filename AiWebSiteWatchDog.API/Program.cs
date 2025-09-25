using Hangfire;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Hangfire.SQLite;
using AiWebSiteWatchDog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/AiWebSiteWatchDog.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Simple CLI utility mode: generate encryption key and exit
if (args.Length == 1 && string.Equals(args[0], "--generate-encryption-key", StringComparison.OrdinalIgnoreCase))
{
    var bytes = new byte[32]; // 256-bit AES key
    System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
    var b64 = Convert.ToBase64String(bytes);
    Console.WriteLine("Base64 AES-256 key (store in GOOGLE_TOKENS_ENCRYPTION_KEY):\n" + b64);
    Console.WriteLine("Length (bytes): 32  | IMPORTANT: keep this stable across deployments.");
    return; // exit process
}

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add Hangfire services (after builder declaration)
builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
    config.UseSQLiteStorage("Data Source=AiWebSiteWatchdog.db;Cache=Shared;Mode=ReadWriteCreate;", new Hangfire.SQLite.SQLiteStorageOptions());
});
builder.Services.AddHangfireServer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Register EF Core DbContext
builder.Services.AddDbContext<AiWebSiteWatchDog.Infrastructure.Persistence.AppDbContext>(options =>
    options.UseSqlite("Data Source=AiWebSiteWatchdog.db"));

// Register infrastructure implementations
builder.Services.AddScoped<ISettingsRepository, AiWebSiteWatchDog.Infrastructure.Persistence.SQLiteSettingsRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.EmailSettingsRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Auth.IGoogleCredentialProvider, AiWebSiteWatchDog.Infrastructure.Auth.GoogleCredentialProvider>();
builder.Services.AddScoped<IEmailSender, AiWebSiteWatchDog.Infrastructure.Email.EmailSender>();
builder.Services.AddScoped<IGeminiApiClient, AiWebSiteWatchDog.Infrastructure.Gemini.GeminiApiClient>();

// Register application services
builder.Services.AddScoped<ISettingsService, AiWebSiteWatchDog.Application.Services.SettingsService>();
builder.Services.AddScoped<INotificationService, AiWebSiteWatchDog.Application.Services.NotificationService>();
builder.Services.AddScoped<IWatcherService, AiWebSiteWatchDog.Application.Services.WatcherService>();

var app = builder.Build();

// Enable Hangfire dashboard (for job monitoring)
app.UseHangfireDashboard();

// Ensure database is migrated and tables are created
AiWebSiteWatchDog.Infrastructure.Persistence.DbInitializer.EnsureMigrated(app.Services);

// Schedule the watch task using Hangfire
using (var scope = app.Services.CreateScope())
{
    var watcherService = scope.ServiceProvider.GetRequiredService<IWatcherService>();
    var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
    var settings = settingsService.GetSettingsAsync().GetAwaiter().GetResult();
    if (!string.IsNullOrWhiteSpace(settings?.Schedule))
    {
        // Basic validation: cron must be 5 or 6 space-separated parts (standard or with seconds)
        var parts = settings.Schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 5 || parts.Length == 6)
        {
            try
            {
                RecurringJob.AddOrUpdate("WatchTaskJob", () => watcherService.CheckWebsiteAsync(settings), settings.Schedule);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to schedule watch task with cron expression: {Schedule}", settings.Schedule);
            }
        }
        else
        {
            Log.Warning("Invalid cron expression (wrong number of fields) in user settings: {Schedule}. Expected 5 or 6 fields. Skipping scheduling.", settings.Schedule);
        }
    }
}

// Enable Swagger middleware
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Register API endpoints from separate file
AiWebSiteWatchDog.API.Endpoints.MapApiEndpoints(app);

app.Run();
