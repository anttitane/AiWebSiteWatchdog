using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace AiWebSiteWatchDog.API.Configuration
{
    public static class ForwardedHeadersExtensions
    {
        public static void UseConfiguredForwardedHeaders(this WebApplication app)
        {
            var cfg = new ForwardedHeadersOptionsConfig();
            app.Configuration.GetSection("ForwardedHeaders").Bind(cfg);

            if (!cfg.Enabled)
            {
                return; // user explicitly disabled forwarded headers
            }

            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            if (cfg.ForwardLimit.HasValue)
            {
                options.ForwardLimit = Math.Max(1, cfg.ForwardLimit.Value);
            }

            // Add known proxies
            foreach (var proxy in cfg.KnownProxies.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (IPAddress.TryParse(proxy, out var ip))
                {
                    options.KnownProxies.Add(ip);
                }
            }

            // Add known networks (CIDR)
            foreach (var cidr in cfg.KnownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var parts = cidr.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip) && int.TryParse(parts[1], out var prefix))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, prefix));
                }
            }

            // If nothing configured, fall back to loopback only (safe for local dev)
            if (options.KnownProxies.Count == 0 && options.KnownNetworks.Count == 0)
            {
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
            }

            app.UseForwardedHeaders(options);
        }
    }
}
