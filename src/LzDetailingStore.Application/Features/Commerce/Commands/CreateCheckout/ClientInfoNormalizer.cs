using System.Net;
using System.Net.Sockets;
using LzDetailingStore.Domain.Entities.Commerce;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;

/// <summary>
/// Normalizes the buyer's IP and User-Agent captured at checkout. Never throws: anything missing or
/// unparseable becomes null so client metadata can never make an order fail.
/// </summary>
public static class ClientInfoNormalizer
{
    /// <summary>
    /// Returns the canonical text of the IP, with IPv4-mapped IPv6 addresses (<c>::ffff:1.2.3.4</c>)
    /// converted to plain IPv4 and any IPv6 scope id removed. Null when missing or not a valid IP.
    /// </summary>
    public static string? NormalizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip.Trim(), out var address))
            return null;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        else if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId != 0)
            address = new IPAddress(address.GetAddressBytes());

        var text = address.ToString();
        return text.Length <= Order.ClientIpMaxLength ? text : null;
    }

    /// <summary>Trims the User-Agent and truncates it to the column length. Null when missing or blank.</summary>
    public static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        var trimmed = userAgent.Trim();
        return trimmed.Length <= Order.ClientUserAgentMaxLength
            ? trimmed
            : trimmed[..Order.ClientUserAgentMaxLength];
    }
}
