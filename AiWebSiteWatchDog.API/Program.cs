using Hangfire;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.Extensions.Configuration;
using Hangfire.SQLite;
using AiWebSiteWatchDog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using AiWebSiteWatchDog.API.Jobs;
using Microsoft.AspNetCore.HttpOverrides;
using AiWebSiteWatchDog.API.Configuration;

// Serilog will be configured from appsettings.json via UseSerilog below so
// logging configuration can be adjusted without recompiling.

// Simple CLI utility mode: generate encryption key and exit
if (Array.Exists(args, a => string.Equals(a, "--generate-encryption-key", StringComparison.OrdinalIgnoreCase)))
{
    var bytes = new byte[32]; // 256-bit AES key
    System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
    var b64 = Convert.ToBase64String(bytes);
    Console.WriteLine("Base64 AES-256 key (store in GOOGLE_TOKENS_ENCRYPTION_KEY):\n" + b64);
    Console.WriteLine("Length (bytes): 32  | IMPORTANT: keep this stable across deployments.");
    return; // exit process
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
// Replace the default logging with Serilog configured from appsettings.json
builder.Host.UseSerilog((ctx, services, loggerConfig) =>
{
    // Ensure Serilog reads from the same configuration source the app uses
    loggerConfig.ReadFrom.Configuration(ctx.Configuration);
    loggerConfig.Enrich.FromLogContext();
});

// Add Hangfire services (after builder declaration)
// Configure Hangfire using connection string from configuration
builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
    var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
                      ?? builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=AiWebSiteWatchdog.db;Cache=Shared;Mode=ReadWriteCreate;";
    config.UseSQLiteStorage(hangfireConn, new Hangfire.SQLite.SQLiteStorageOptions());
});
builder.Services.AddHangfireServer(options =>
{
    // Reduce parallelism to minimize duplicate concurrent executions
    options.WorkerCount = builder.Configuration.GetValue<int?>("Hangfire:WorkerCount") ?? 1;
});
builder.Services.AddEndpointsApiExplorer();
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

// Register EF Core DbContext using connection string from configuration
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=AiWebSiteWatchdog.db";
builder.Services.AddDbContext<AiWebSiteWatchDog.Infrastructure.Persistence.AppDbContext>(options =>
    options.UseSqlite(defaultConn));

// Add in-memory cache for settings caching
builder.Services.AddMemoryCache();

// Add rate limiting from configuration
builder.Services.AddConfiguredRateLimiting(builder.Configuration);

// Register infrastructure implementations
builder.Services.AddScoped<ISettingsRepository, AiWebSiteWatchDog.Infrastructure.Persistence.SQLiteSettingsRepository>();
builder.Services.AddScoped<INotificationRepository, AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository>();
builder.Services.AddScoped<AiWebSiteWatchDog.Infrastructure.Auth.IGoogleCredentialProvider, AiWebSiteWatchDog.Infrastructure.Auth.GoogleCredentialProvider>();
builder.Services.AddScoped<IEmailSender, AiWebSiteWatchDog.Infrastructure.Email.EmailSender>();
builder.Services.AddHttpClient<IGeminiApiClient, AiWebSiteWatchDog.Infrastructure.Gemini.GeminiApiClient>();

// Register application services
builder.Services.AddScoped<ISettingsService, AiWebSiteWatchDog.Application.Services.SettingsService>();
builder.Services.AddScoped<INotificationService, AiWebSiteWatchDog.Application.Services.NotificationService>();
builder.Services.AddScoped<IWatcherService, AiWebSiteWatchDog.Application.Services.WatcherService>();
// Register Hangfire job runner
builder.Services.AddScoped<WatchTaskJobRunner>();

var app = builder.Build();

// Honor proxy forwarded headers, but only from configured/known proxies or networks
app.UseConfiguredForwardedHeaders();

// In development, expose Hangfire Dashboard (before rate limiting to avoid throttling UI polling)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Enable rate limiting middleware
app.UseConfiguredRateLimiting();

// Serve static frontend (React) if present
app.UseDefaultFiles();
app.UseStaticFiles();

// Ensure database is migrated and tables are created
AiWebSiteWatchDog.Infrastructure.Persistence.DbInitializer.EnsureMigrated(app.Services);

// Schedule the watch task using Hangfire
using (var scope = app.Services.CreateScope())
{
    var taskRepo = scope.ServiceProvider.GetRequiredService<AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository>();
    var tasks = taskRepo.GetAllAsync().GetAwaiter().GetResult();
    foreach (var t in tasks)
    {
        var recurringId = $"WatchTask_{t.Id}";
        if (!t.Enabled)
        {
            // Ensure disabled tasks are not scheduled
            RecurringJob.RemoveIfExists(recurringId);
            continue;
        }
        if (string.IsNullOrWhiteSpace(t.Schedule))
        {
            // No schedule provided -> ensure no lingering job exists
            RecurringJob.RemoveIfExists(recurringId);
            continue;
        }
        var parts = t.Schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 5 or 6)
        {
                try
                {
                    var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Local };
                    RecurringJob.AddOrUpdate<WatchTaskJobRunner>(recurringId, r => r.ExecuteAsync(t.Id), t.Schedule, options);
                }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to schedule watch task {TaskId} with cron expression: {Schedule}", t.Id, t.Schedule);
            }
        }
        else
        {
            Log.Warning("Invalid cron expression for watch task {TaskId}: {Schedule}. Expected 5 or 6 fields. Skipping.", t.Id, t.Schedule);
            // Also make sure any existing job is removed
            RecurringJob.RemoveIfExists(recurringId);
        }
    }
}

// Register API endpoints from separate file
AiWebSiteWatchDog.API.Endpoints.MapApiEndpoints(app);

// Enable Swagger middleware AFTER endpoints are mapped so the OpenAPI generator
// can pick up minimal API endpoints and produce a valid document.
// TODO: restrict Swagger to non-production environments only!
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// SPA fallback: route unknown paths to index.html so client-side routing works
app.MapFallbackToFile("/index.html");

app.Run();
