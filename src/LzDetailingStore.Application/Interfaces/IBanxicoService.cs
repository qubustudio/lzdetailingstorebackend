using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Application.Interfaces;

public interface IBanxicoService
{
    Task<Result<decimal>> GetCurrentRateAsync(CancellationToken ct = default);
}
