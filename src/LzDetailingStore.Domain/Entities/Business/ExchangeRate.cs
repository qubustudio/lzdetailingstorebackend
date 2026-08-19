using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Business;

public sealed class ExchangeRate : BaseEntity
{
    public decimal Rate { get; set; }
    public string Source { get; set; } = "Banxico";
    public DateTime FetchedAt { get; set; }
}
