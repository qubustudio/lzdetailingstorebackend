using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Application.Interfaces;

public record MercadoPagoItem(string Title, int Quantity, decimal UnitPrice);

public record MercadoPagoBackUrls(string Success, string Failure, string Pending);

public record MercadoPagoPaymentInfo(
    string PaymentId,
    string Status,
    decimal Amount,
    string? ExternalReference);

public interface IMercadoPagoService
{
    Task<Result<string>> CreatePreferenceAsync(
        string externalReference,
        IList<MercadoPagoItem> items,
        MercadoPagoBackUrls backUrls,
        CancellationToken ct = default);

    bool ValidateWebhookSignature(string xSignature, string xRequestId, string dataId);

    Task<Result<MercadoPagoPaymentInfo>> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}
