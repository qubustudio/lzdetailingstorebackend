using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Commerce;

public sealed class Cart : BaseEntity
{
    public Guid UserId { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
