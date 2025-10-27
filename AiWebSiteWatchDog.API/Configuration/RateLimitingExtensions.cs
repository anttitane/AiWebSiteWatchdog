using System;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;

namespace AiWebSiteWatchDog.API.Configuration
{
    public static class RateLimitingExtensions
    {
        private static string GetClientKey(HttpContext ctx)
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrEmpty(ip) ? "unknown" : ip;
        }

        public static IServiceCollection AddConfiguredRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var opts = new RateLimitingOptions();
            configuration.GetSection("RateLimiting").Bind(opts);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.Headers["Retry-After"] = opts.Rejection.RetryAfterSeconds.ToString();
                    await context.HttpContext.Response.WriteAsync(opts.Rejection.Message, token);
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetClientKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, opts.Global.PermitLimit),
                            Window = TimeSpan.FromSeconds(Math.Max(1, opts.Global.WindowSeconds)),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = Math.Max(0, opts.Global.QueueLimit)
                        }));

                options.AddPolicy("StrictPerIp", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, opts.StrictPerIp.PermitLimit),
                            Window = TimeSpan.FromSeconds(Math.Max(1, opts.StrictPerIp.WindowSeconds)),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = Math.Max(0, opts.StrictPerIp.QueueLimit)
                        }));

                options.AddPolicy("RunTaskPerIp", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, opts.RunTaskPerIp.PermitLimit),
                            Window = TimeSpan.FromSeconds(Math.Max(1, opts.RunTaskPerIp.WindowSeconds)),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = Math.Max(0, opts.RunTaskPerIp.QueueLimit)
                        }));

                options.AddPolicy("RunTaskConcurrencyPerIp", httpContext =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        GetClientKey(httpContext),
                        _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = Math.Max(1, opts.RunTaskConcurrencyPerIp.PermitLimit),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = Math.Max(0, opts.RunTaskConcurrencyPerIp.QueueLimit)
                        }));
            });

            return services;
        }

        public static void UseConfiguredRateLimiting(this Microsoft.AspNetCore.Builder.WebApplication app)
        {
            app.UseRateLimiter();
        }
    }
}
