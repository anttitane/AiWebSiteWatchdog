using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using AiWebSiteWatchDog.Domain.Entities;
using AiWebSiteWatchDog.Domain.DTOs;
using AiWebSiteWatchDog.Domain.Interfaces;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using AiWebSiteWatchDog.Application.Parsing;
using Hangfire;
using AiWebSiteWatchDog.API.Jobs;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;

namespace AiWebSiteWatchDog.API
{
    public static class Endpoints
    {
        public static void MapApiEndpoints(this WebApplication app)
        {
            // User settings endpoints
            app.MapGet("/settings", async ([FromServices] ISettingsService settingsService) =>
            {
                var s = await settingsService.GetSettingsAsync();
                return s is null ? Results.NotFound() : Results.Ok(s.ToDto());
            })
            .WithName("GetSettings")
            .WithTags("Settings")
            .Produces<UserSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/settings", async ([FromServices] ISettingsService settingsService, UserSettings settings) =>
            {
                await settingsService.SaveSettingsAsync(settings);
                return Results.Ok();
            })
            .WithName("UpdateSettings")
            .WithTags("Settings")
            .Accepts<UserSettings>("application/json")
            .Produces(StatusCodes.Status200OK);

            // Watch tasks endpoints
            app.MapGet("/tasks", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo) =>
            {
                var tasks = await repo.GetAllAsync();
                return Results.Ok(tasks.Select(t => t.ToDto()));
            })
            .WithName("ListTasks")
            .WithTags("Tasks")
            .Produces<IEnumerable<WatchTaskDto>>(StatusCodes.Status200OK);

            app.MapPost("/tasks", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, CreateWatchTaskRequest request) =>
            {
                // Validation: Title, Url, TaskPrompt are required; Title max 200 chars
                var errors = new Dictionary<string, string[]>();
                if (string.IsNullOrWhiteSpace(request.Title))
                    errors["Title"] = ["Title is required."];
                else if (request.Title.Length > 200)
                    errors["Title"] = ["Title must be 200 characters or fewer."];
                if (string.IsNullOrWhiteSpace(request.Url))
                    errors["Url"] = ["Url is required."];
                if (string.IsNullOrWhiteSpace(request.TaskPrompt))
                    errors["TaskPrompt"] = ["TaskPrompt is required."];
                if (errors.Count > 0) return Results.ValidationProblem(errors);
                
                // Map DTO to entity and let DB generate Id
                var task = new WatchTask
                {
                    Id = 0,
                    Title = request.Title.Trim(),
                    Url = request.Url,
                    TaskPrompt = request.TaskPrompt,
                    Schedule = request.Schedule ?? string.Empty,
                    LastChecked = default,
                    LastResult = null,
                    Enabled = request.Enabled ?? true
                };

                await repo.AddAsync(task);
                // If a valid schedule is provided, schedule immediately
                if (task.Enabled && !string.IsNullOrWhiteSpace(task.Schedule))
                {
                    var parts = task.Schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length is 5 or 6)
                    {
                        try
                        {
                            var recurringId = $"WatchTask_{task.Id}";
                            var _opts = new RecurringJobOptions { TimeZone = TimeZoneInfo.Local };
                            RecurringJob.AddOrUpdate<WatchTaskJobRunner>(recurringId, r => r.ExecuteAsync(task.Id), task.Schedule, _opts);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to schedule new task {TaskId} with cron expression: {Schedule}", task.Id, task.Schedule);
                        }
                    }
                }
                var dto = task.ToDto();
                return Results.Created($"/tasks/{dto.Id}", dto);
            })
            .WithName("CreateTask")
            .WithTags("Tasks")
            .Accepts<CreateWatchTaskRequest>("application/json")
            .Produces<WatchTaskDto>(StatusCodes.Status201Created)
            .WithDescription("Create a watch task. Required: title (max 200), url, taskPrompt. Optional: schedule, enabled (default true). NOTE: Id is auto-generated and ignored. Example body: {\n  \"title\": \"Find the date for latest update\",\n  \"url\": \"https://example.com/news\",\n  \"taskPrompt\": \"From the website text, extract the latest navigation update date.\",\n  \"schedule\": \"0 8 * * *\",\n  \"enabled\": true\n}");

            app.MapGet("/tasks/{id}", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id) =>
            {
                var task = await repo.GetByIdAsync(id);
                return task is not null ? Results.Ok(task.ToDto()) : Results.NotFound();
            })
            .WithName("GetTaskById")
            .WithTags("Tasks")
            .Produces<WatchTaskDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/tasks/{id}", async (
                [FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo,
                int id,
                UpdateWatchTaskRequest updated) =>
            {
                var existing = await repo.GetByIdAsync(id);
                if (existing is null) return Results.NotFound();

                // Validation: all fields optional; if Title provided, enforce max length and non-empty
                var errors = new Dictionary<string, string[]>();
                if (updated.Title != null)
                {
                    if (string.IsNullOrWhiteSpace(updated.Title)) errors["Title"] = ["Title is required."];
                    else if (updated.Title.Length > 200) errors["Title"] = ["Title must be 200 characters or fewer."];
                }
                if (updated.Schedule != null)
                {
                    var parts = updated.Schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (!(parts.Length is 5 or 6))
                        errors["Schedule"] = ["Invalid cron expression. Expected 5 or 6 space-separated fields (e.g., '*/15 * * * *' or '0 8 * * *')."];
                }
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                // Map fields if provided
                if (updated.Title != null) existing.Title = updated.Title.Trim();
                if (updated.Url != null) existing.Url = updated.Url;
                if (updated.TaskPrompt != null) existing.TaskPrompt = updated.TaskPrompt;
                if (updated.Schedule != null) existing.Schedule = updated.Schedule;
                if (updated.LastChecked.HasValue) existing.LastChecked = updated.LastChecked.Value;
                if (updated.LastResult != null) existing.LastResult = updated.LastResult;
                if (updated.Enabled.HasValue) existing.Enabled = updated.Enabled.Value;

                var result = await repo.UpdateAsync(id, existing);
                if (!result) return Results.NotFound();
                var refreshed = await repo.GetByIdAsync(id);
                if (refreshed is null) return Results.NotFound();

                // If schedule or enabled changed, reconcile the recurring job immediately
                if (updated.Schedule != null || updated.Enabled.HasValue)
                {
                    try
                    {
                        var recurringId = $"WatchTask_{id}";
                        var parts2 = (refreshed.Schedule ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (refreshed.Enabled && parts2.Length is 5 or 6)
                        {
                            var _opts2 = new RecurringJobOptions { TimeZone = TimeZoneInfo.Local };
                            RecurringJob.AddOrUpdate<WatchTaskJobRunner>(recurringId, r => r.ExecuteAsync(id), refreshed.Schedule, _opts2);
                        }
                        else
                        {
                            RecurringJob.RemoveIfExists(recurringId);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the API response; surface scheduling issues via logs
                        Log.Warning(ex, "Failed to (re)schedule task {TaskId} with cron expression: {Schedule}", id, refreshed.Schedule);
                    }
                }

                return Results.Ok(refreshed.ToDto());
            })
            .WithName("UpdateTask")
            .WithTags("Tasks")
            .Accepts<UpdateWatchTaskRequest>("application/json")
            .Produces<WatchTaskDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithDescription("Update a watch task. All fields are optional; only provided fields are updated. If title is provided, it must be non-empty and max 200. Example body (title only): {\n  \"title\": \"Find the date for latest update\"\n}");

            // Unified delete: single or multiple IDs via comma-separated path parameter
            // Matches: /tasks/1 or /tasks/1,2,3
            app.MapDelete("/tasks/{ids:regex(^[0-9]+(,[0-9]+)*$)}", async (
                [FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo,
                string ids) =>
            {
                var parsed = ids
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (parsed.Count == 0)
                {
                    return Results.BadRequest(new { error = "Provide one or more numeric task IDs in the path, e.g., /tasks/1 or /tasks/1,2,3" });
                }

                var (deletedIds, notFoundIds) = await repo.DeleteManyAsync(parsed);

                foreach (var did in deletedIds)
                {
                    RecurringJob.RemoveIfExists($"WatchTask_{did}");
                }

                return Results.Ok(new { deletedIds, notFoundIds });
            })
            .WithName("DeleteTasks")
            .WithTags("Tasks")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithDescription("Delete one or multiple tasks using a comma-separated list of IDs in the path. Examples: DELETE /tasks/1 or DELETE /tasks/1,2,3");

            // Manual trigger endpoint
            app.MapPost("/tasks/{id}/run", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo,
                                                    [FromServices] IWatcherService watcherService,
                                                    [FromServices] INotificationRepository notifications,
                                                    [FromServices] INotificationService notificationService,
                                                    int id,
                                                    [FromQuery] bool sendEmail = false) =>
            {
                var task = await repo.GetByIdAsync(id);
                if (task is null) return Results.NotFound();

                try
                {
                    var updated = await watcherService.CheckWebsiteAsync(task);
                    // Persist the updated task state
                    await repo.UpdateAsync(id, updated);

                    // Save a notification record with the Gemini result (no email send here)
                    var subject = $"AiWebSiteWatchDog results for task - {updated.Title}";
                    var message = GeminiResponseParser.ExtractText(updated.LastResult) ?? "(no content)";
                    if (sendEmail)
                    {
                        await notificationService.SendNotificationAsync(new CreateNotificationRequest(subject, message));
                    }
                    else
                    {
                        var notification = new Notification(0, subject, message, DateTime.UtcNow);
                        await notifications.AddAsync(notification);
                    }

                    return Results.Ok(updated.ToDto());
                }
                catch (Exception ex)
                {
                    var correlationId = await AiWebSiteWatchDog.API.Utils.FailureHandler.HandleFailureAsync(ex, task, id, repo, notificationService, notifications, "manual run");
                    return Results.Problem(title: "Task run failed", detail: $"An internal error occurred. Reference: {correlationId}", statusCode: 500);
                }
            })
            .WithName("RunTask")
            .WithTags("Tasks")
            .WithDescription("Manually run a watch task. Optional query ?sendEmail=true to also send an email notification.")
            .RequireRateLimiting("RunTaskConcurrencyPerIp")
            .Produces<WatchTaskDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // Notifications endpoints
            app.MapGet("/notifications", async ([FromServices] INotificationRepository repo) =>
            {
                var list = await repo.GetAllAsync();
                return Results.Ok(list.Select(n => n.ToDto()));
            })
            .WithName("ListNotifications")
            .WithTags("Notifications")
            .Produces<IEnumerable<NotificationDto>>(StatusCodes.Status200OK);

            app.MapGet("/notifications/{id}", async ([FromServices] INotificationRepository repo, int id) =>
            {
                var notification = await repo.GetByIdAsync(id);
                return notification is not null ? Results.Ok(notification.ToDto()) : Results.NotFound();
            })
            .WithName("GetNotificationById")
            .WithTags("Notifications")
            .Produces<NotificationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // Unified delete for notifications: single or multiple IDs via comma-separated path
            app.MapDelete("/notifications/{ids:regex(^[0-9]+(,[0-9]+)*$)}", async ([FromServices] INotificationRepository repo, string ids) =>
            {
                var parsed = ids
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (parsed.Count == 0)
                {
                    return Results.BadRequest(new { error = "Provide one or more numeric notification IDs in the path, e.g., /notifications/1 or /notifications/1,2,3" });
                }

                if (repo is AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository concrete)
                {
                    var (deletedIds, notFoundIds) = await concrete.DeleteManyAsync(parsed);
                    return Results.Ok(new { deletedIds, notFoundIds });
                }
                else
                {
                    // Fallback if using an implementation without bulk method
                    var deleted = new List<int>();
                    var missing = new List<int>();
                    foreach (var id in parsed)
                    {
                        var ok = await repo.DeleteAsync(id);
                        if (ok) deleted.Add(id); else missing.Add(id);
                    }
                    return Results.Ok(new { deletedIds = deleted, notFoundIds = missing });
                }
            })
            .WithName("DeleteNotifications")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithDescription("Delete one or multiple notifications using a comma-separated list of IDs in the path. Examples: DELETE /notifications/1 or DELETE /notifications/1,2,3");

            // Delete all notifications
            app.MapDelete("/notifications", async ([FromServices] INotificationRepository repo) =>
            {
                if (repo is AiWebSiteWatchDog.Infrastructure.Persistence.NotificationRepository concrete)
                {
                    var count = await concrete.DeleteAllAsync();
                    return Results.Ok(new { deletedCount = count });
                }
                else
                {
                    // Fallback: delete individually
                    var list = await repo.GetAllAsync();
                    var deleted = 0;
                    foreach (var n in list)
                    {
                        if (await repo.DeleteAsync(n.Id)) deleted++;
                    }
                    return Results.Ok(new { deletedCount = deleted });
                }
            })
            .WithName("DeleteAllNotifications")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status200OK)
            .WithDescription("Delete all notifications in the system.");

            // Send notification (trigger email)
            app.MapPost("/notifications", async ([FromServices] INotificationService notificationService, CreateNotificationRequest request) =>
            {
                var dto = await notificationService.SendNotificationAsync(request);
                return Results.Created($"/notifications/{dto.Id}", dto);
            })
            .WithName("SendNotification")
            .WithTags("Notifications")
            .Accepts<CreateNotificationRequest>("application/json")
            .Produces<NotificationDto>(StatusCodes.Status201Created);

            // Health/status endpoint
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).DisableRateLimiting();

            // Delete all tasks
            app.MapDelete("/tasks", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo) =>
            {
                // Remove recurring jobs first using current IDs
                var tasks = await repo.GetAllAsync();
                foreach (var t in tasks)
                {
                    RecurringJob.RemoveIfExists($"WatchTask_{t.Id}");
                }

                var count = await repo.DeleteAllAsync();
                return Results.Ok(new { deletedCount = count });
            })
            .WithName("DeleteAllTasks")
            .WithTags("Tasks")
            .Produces(StatusCodes.Status200OK)
            .WithDescription("Delete all tasks and remove their scheduled jobs.");

            // Authentication initiation endpoint (triggers Google OAuth consent if not already authorized)
            // Optional query parameter ?senderEmail= overrides stored settings sender email.
            app.MapPost("/auth/initiate", async (
                [FromServices] Infrastructure.Auth.IGoogleCredentialProvider credentialProvider,
                [FromServices] ISettingsService settingsService,
                HttpRequest request,
                CancellationToken ct) =>
            {
                // Allow explicit sender email override via query string
                var senderValues = request.Query["senderEmail"];
                string? senderEmail = senderValues.Count > 0 ? senderValues[0] : null;
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    var settings = await settingsService.GetSettingsAsync();
                    if (settings is null || string.IsNullOrWhiteSpace(settings.SenderEmail))
                        return Results.BadRequest("Sender email not configured and no senderEmail query parameter provided.");
                    senderEmail = settings.SenderEmail;
                }

                try
                {
                    await credentialProvider.GetGmailAndGeminiCredentialAsync(senderEmail!, ct);
                    return Results.Ok(new { senderEmail, authenticated = true });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            })
            .WithDescription("Initiate Google OAuth consent for Gmail/Gemini scopes.")
            .RequireRateLimiting("StrictPerIp")
            .WithTags("auth");
        } 
    }
}
