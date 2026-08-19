namespace LzDetailingStore.Application.Interfaces;

public record BulkImportResult(int TotalRows, int SuccessCount, int ErrorCount, List<BulkImportError> Errors);

public record BulkImportError(int Row, string Field, string Message);

public interface IBulkImportService
{
    Task<BulkImportResult> ProcessProductsAsync(Stream fileStream, CancellationToken ct = default);
    Task<BulkImportResult> ProcessBrandsAsync(Stream fileStream, CancellationToken ct = default);
    Task<BulkImportResult> ProcessCategoriesAsync(Stream fileStream, CancellationToken ct = default);
}
