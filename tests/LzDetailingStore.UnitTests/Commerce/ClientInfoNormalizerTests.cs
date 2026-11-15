using FluentAssertions;
using LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;
using LzDetailingStore.Domain.Entities.Commerce;

namespace LzDetailingStore.UnitTests.Commerce;

public sealed class ClientInfoNormalizerTests
{
    [Theory]
    [InlineData("201.141.10.20", "201.141.10.20")]
    [InlineData("::ffff:189.203.5.7", "189.203.5.7")]
    [InlineData(" 10.0.0.1 ", "10.0.0.1")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    [InlineData("fe80::1%12", "fe80::1")]
    public void NormalizeIp_DevuelveIpCanonica(string input, string expected) =>
        ClientInfoNormalizer.NormalizeIp(input).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-una-ip")]
    [InlineData("999.1.1.1")]
    public void NormalizeIp_FaltanteOInvalida_DevuelveNulo(string? input) =>
        ClientInfoNormalizer.NormalizeIp(input).Should().BeNull();

    [Fact]
    public void NormalizeIp_IpV6MasLarga_CabeEnLaColumna()
    {
        var ip = ClientInfoNormalizer.NormalizeIp("::ffff:255.255.255.255");

        ip.Should().Be("255.255.255.255");
        ClientInfoNormalizer.NormalizeIp("2001:db8:85a3:1234:5678:8a2e:370:7334")!.Length
            .Should().BeLessThanOrEqualTo(Order.ClientIpMaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeUserAgent_FaltanteOVacio_DevuelveNulo(string? input) =>
        ClientInfoNormalizer.NormalizeUserAgent(input).Should().BeNull();

    [Fact]
    public void NormalizeUserAgent_RecortaEspacios() =>
        ClientInfoNormalizer.NormalizeUserAgent("  Mozilla/5.0  ").Should().Be("Mozilla/5.0");

    [Fact]
    public void NormalizeUserAgent_DemasiadoLargo_SeTruncaAlLimiteDeLaColumna()
    {
        var userAgent = new string('x', Order.ClientUserAgentMaxLength) + "sobrante";

        ClientInfoNormalizer.NormalizeUserAgent(userAgent).Should().Be(new string('x', Order.ClientUserAgentMaxLength));
    }
}
