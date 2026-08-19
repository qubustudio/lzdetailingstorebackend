namespace LzDetailingStore.Application.Interfaces;

public interface IBulkExportService
{
    Task<byte[]> ExportProductsAsync(CancellationToken ct = default);
    Task<byte[]> ExportBrandsAsync(CancellationToken ct = default);
    Task<byte[]> ExportCategoriesAsync(CancellationToken ct = default);
    byte[] GetTemplate(string type);
}
