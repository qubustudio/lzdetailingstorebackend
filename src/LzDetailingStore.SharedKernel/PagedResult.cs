namespace LzDetailingStore.SharedKernel;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    int Total,
    int Page,
    int TotalPages);
