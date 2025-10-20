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
                await repo.AddAsync(task);
                var dto = task.ToDto();
                return Results.Created($"/tasks/{dto.Id}", dto);
            })
            .WithName("CreateTask")
            .WithTags("Tasks")
            .Accepts<WatchTask>("application/json")
            .Produces<WatchTaskDto>(StatusCodes.Status201Created);

            app.MapGet("/tasks/{id}", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id) =>
            {
                var task = await repo.GetByIdAsync(id);
                return task is not null ? Results.Ok(task.ToDto()) : Results.NotFound();
            })
            .WithName("GetTaskById")
            .WithTags("Tasks")
            .Produces<WatchTaskDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/tasks/{id}", async ([FromServices] AiWebSiteWatchDog.Infrastructure.Persistence.WatchTaskRepository repo, int id, WatchTask updated) =>
            {
                var result = await repo.UpdateAsync(id, updated);
                if (!result) return Results.NotFound();
                var task = await repo.GetByIdAsync(id);
                return task is null ? Results.NotFound() : Results.Ok(task.ToDto());
            })
            .WithName("UpdateTask")
            .WithTags("Tasks")
            .Accepts<WatchTask>("application/json")
            .Produces<WatchTaskDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

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
                var subject = $"WatchTask {updated.Id} result";
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
