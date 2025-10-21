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

            app.MapPost("/tasks", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, WatchTask task) =>
            {
                // Validation: Title, Url, TaskPrompt are required; Title max 200 chars
                var errors = new Dictionary<string, string[]>();
                if (string.IsNullOrWhiteSpace(task.Title))
                    errors["Title"] = ["Title is required."];
                else if (task.Title.Length > 200)
                    errors["Title"] = ["Title must be 200 characters or fewer."];
                if (string.IsNullOrWhiteSpace(task.Url))
                    errors["Url"] = ["Url is required."];
                if (string.IsNullOrWhiteSpace(task.TaskPrompt))
                    errors["TaskPrompt"] = ["TaskPrompt is required."];
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                await repo.AddAsync(task);
                // If a valid schedule is provided, schedule immediately
                if (!string.IsNullOrWhiteSpace(task.Schedule))
                {
                    var parts = task.Schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length is 5 or 6)
                    {
                        try
                        {
                            var recurringId = $"WatchTask_{task.Id}";
                            RecurringJob.AddOrUpdate<WatchTaskJobRunner>(recurringId, r => r.ExecuteAsync(task.Id), task.Schedule);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARN] Failed to schedule new task {task.Id}: {ex.Message}");
                        }
                    }
                }
                var dto = task.ToDto();
                return Results.Created($"/tasks/{dto.Id}", dto);
            })
            .WithName("CreateTask")
            .WithTags("Tasks")
            .Accepts<WatchTask>("application/json")
            .Produces<WatchTaskDto>(StatusCodes.Status201Created)
            .WithDescription("Create a watch task. Required: title (max 200), url, taskPrompt. Optional: schedule. Example body: {\n  \"title\": \"Find the date for latest update\",\n  \"url\": \"https://example.com/news\",\n  \"taskPrompt\": \"From the website text, extract the latest navigation update date.\",\n  \"schedule\": \"0 8 * * *\"\n}");

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

                var result = await repo.UpdateAsync(id, existing);
                if (!result) return Results.NotFound();
                var refreshed = await repo.GetByIdAsync(id);
                if (refreshed is null) return Results.NotFound();

                // If a new schedule was provided, (re)schedule the recurring job immediately
                if (updated.Schedule != null)
                {
                    try
                    {
                        var recurringId = $"WatchTask_{id}";
                        RecurringJob.AddOrUpdate<WatchTaskJobRunner>(recurringId, r => r.ExecuteAsync(id), refreshed.Schedule);
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the API response; surface scheduling issues via logs
                        Console.WriteLine($"[WARN] Failed to (re)schedule task {id}: {ex.Message}");
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

            app.MapDelete("/tasks/{id}", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id) =>
            {
                var result = await repo.DeleteAsync(id);
                return result ? Results.Ok() : Results.NotFound();
            })
            .WithName("DeleteTask")
            .WithTags("Tasks")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

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
            })
            .WithName("RunTask")
            .WithTags("Tasks")
            .WithDescription("Manually run a watch task. Optional query ?sendEmail=true to also send an email notification.")
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

            app.MapDelete("/notifications/{id}", async ([FromServices] INotificationRepository repo, int id) =>
            {
                var result = await repo.DeleteAsync(id);
                return result ? Results.Ok() : Results.NotFound();
            })
            .WithName("DeleteNotification")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

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
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

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
            .WithTags("auth");
        } 
    }
}
