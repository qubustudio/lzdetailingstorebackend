using ClosedXML.Excel;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Infrastructure.Persistence;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class ExcelBulkImportService(
    AppDbContext dbContext,
    ISlugGenerator slugGenerator,
    ILogger<ExcelBulkImportService> logger) : IBulkImportService
{
    public async Task<BulkImportResult> ProcessProductsAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var errors = new List<BulkImportError>();
        var successCount = 0;
        var totalRows = worksheet.LastRowUsed()?.RowNumber() - 1 ?? 0;

        for (var row = 2; row <= totalRows + 1; row++)
        {
            try
            {
                var name = worksheet.Cell(row, 1).GetString().Trim();
                var brandName = worksheet.Cell(row, 2).GetString().Trim();
                var categoryName = worksheet.Cell(row, 3).GetString().Trim();
                var description = worksheet.Cell(row, 4).GetString().Trim();
                var imageUrl = worksheet.Cell(row, 5).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add(new BulkImportError(row, "Name", "El nombre es requerido."));
                    continue;
                }

                var brand = await dbContext.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Nombre.ToLower() == brandName.ToLower() && b.IsActive, ct);

                if (brand is null)
                {
                    errors.Add(new BulkImportError(row, "Brand", $"Marca '{brandName}' no encontrada."));
                    continue;
                }

                var category = await dbContext.Set<Category>()
                    .FirstOrDefaultAsync(c => c.Nombre.ToLower() == categoryName.ToLower() && c.IsActive, ct);

                if (category is null)
                {
                    errors.Add(new BulkImportError(row, "Category", $"Categoría '{categoryName}' no encontrada."));
                    continue;
                }

                var slug = await slugGenerator.GenerateAsync(name, "products", ct);

                var product = new Product
                {
                    Name = name,
                    Slug = slug,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    BrandId = brand.Id,
                    CategoryId = category.Id,
                    ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "https://placeholder.com/product.jpg" : imageUrl,
                    IsActive = true
                };

                await dbContext.Set<Product>().AddAsync(product, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportError(row, "General", ex.Message));
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Products import: {Success}/{Total} rows successful", successCount, totalRows);

        return new BulkImportResult(totalRows, successCount, errors.Count, errors);
    }

    public async Task<BulkImportResult> ProcessBrandsAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var errors = new List<BulkImportError>();
        var successCount = 0;
        var totalRows = worksheet.LastRowUsed()?.RowNumber() - 1 ?? 0;

        for (var row = 2; row <= totalRows + 1; row++)
        {
            try
            {
                var nombre = worksheet.Cell(row, 1).GetString().Trim();
                var descripcion = worksheet.Cell(row, 2).GetString().Trim();
                var categoriaStr = worksheet.Cell(row, 3).GetString().Trim();
                var origen = worksheet.Cell(row, 4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new BulkImportError(row, "Nombre", "El nombre es requerido."));
                    continue;
                }

                var exists = await dbContext.Set<Brand>()
                    .AnyAsync(b => b.Nombre.ToLower() == nombre.ToLower(), ct);

                if (exists)
                {
                    errors.Add(new BulkImportError(row, "Nombre", $"Marca '{nombre}' ya existe."));
                    continue;
                }

                BrandCategory? categoria = null;
                if (!string.IsNullOrWhiteSpace(categoriaStr)
                    && Enum.TryParse<BrandCategory>(categoriaStr, true, out var parsed))
                    categoria = parsed;

                var slug = await slugGenerator.GenerateAsync(nombre, "brands", ct);

                var brand = new Brand
                {
                    Nombre = nombre,
                    Slug = slug,
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                    Categoria = categoria,
                    Origen = string.IsNullOrWhiteSpace(origen) ? null : origen,
                    IsActive = true
                };

                await dbContext.Set<Brand>().AddAsync(brand, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportError(row, "General", ex.Message));
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Brands import: {Success}/{Total} rows successful", successCount, totalRows);

        return new BulkImportResult(totalRows, successCount, errors.Count, errors);
    }

    public async Task<BulkImportResult> ProcessCategoriesAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var errors = new List<BulkImportError>();
        var successCount = 0;
        var totalRows = worksheet.LastRowUsed()?.RowNumber() - 1 ?? 0;

        for (var row = 2; row <= totalRows + 1; row++)
        {
            try
            {
                var nombre = worksheet.Cell(row, 1).GetString().Trim();
                var descripcion = worksheet.Cell(row, 2).GetString().Trim();
                var ordenStr = worksheet.Cell(row, 3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new BulkImportError(row, "Nombre", "El nombre es requerido."));
                    continue;
                }

                var exists = await dbContext.Set<Category>()
                    .AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower(), ct);

                if (exists)
                {
                    errors.Add(new BulkImportError(row, "Nombre", $"Categoría '{nombre}' ya existe."));
                    continue;
                }

                var slug = await slugGenerator.GenerateAsync(nombre, "categories", ct);
                int.TryParse(ordenStr, out var orden);

                var category = new Category
                {
                    Nombre = nombre,
                    Slug = slug,
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                    Orden = orden,
                    IsActive = true
                };

                await dbContext.Set<Category>().AddAsync(category, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkImportError(row, "General", ex.Message));
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Categories import: {Success}/{Total} rows successful", successCount, totalRows);

        return new BulkImportResult(totalRows, successCount, errors.Count, errors);
    }
}
