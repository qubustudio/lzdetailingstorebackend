using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace LzDetailingStore.Api.Extensions;

/// <summary>
/// Configuration of the <c>ForwardedHeaders</c> section. Only the proxies listed here are trusted to set
/// X-Forwarded-For / X-Forwarded-Proto; with nothing configured only loopback is trusted (framework default),
/// so a client can never spoof its IP by sending the header itself.
/// Behind Cloudflare, every Cloudflare IP range (https://www.cloudflare.com/ips/) must be listed in
/// <see cref="KnownNetworks"/>, plus the address of any reverse proxy in front of the API.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Individual trusted proxy IPs (e.g. "10.0.0.5").</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Trusted proxy networks in CIDR notation (e.g. "173.245.48.0/20").</summary>
    public string[] KnownNetworks { get; set; } = [];

    /// <summary>
    /// Max number of X-Forwarded-For entries processed. Null keeps the framework default (1); use 2 when a
    /// reverse proxy sits behind Cloudflare so the chain client → Cloudflare → proxy resolves to the client.
    /// </summary>
    public int? ForwardLimit { get; set; }
}

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddConfiguredForwardedHeaders(
        this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
                       ?? new ForwardedHeadersSettings();

        // Parsed eagerly so a malformed entry fails at startup instead of on the first request.
        var proxies = settings.KnownProxies
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => IPAddress.TryParse(p.Trim(), out var address)
                ? address
                : throw new InvalidOperationException(
                    $"{ForwardedHeadersSettings.SectionName}:KnownProxies contains an invalid IP address: '{p}'."))
            .ToList();

        var networks = settings.KnownNetworks
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => System.Net.IPNetwork.TryParse(n.Trim(), out var network)
                ? network
                : throw new InvalidOperationException(
                    $"{ForwardedHeadersSettings.SectionName}:KnownNetworks contains an invalid CIDR network: '{n}'."))
            .ToList();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Configured proxies are added on top of the loopback defaults; the lists are never cleared,
            // which would make the API trust X-Forwarded-For from any caller (IP spoofing).
            foreach (var proxy in proxies)
                options.KnownProxies.Add(proxy);

            foreach (var network in networks)
                options.KnownIPNetworks.Add(network);

            if (settings.ForwardLimit is { } limit)
                options.ForwardLimit = limit;
        });

        return services;
    }
}
