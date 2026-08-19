using ClosedXML.Excel;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class ExcelBulkExportService(AppDbContext dbContext) : IBulkExportService
{
    public async Task<byte[]> ExportProductsAsync(CancellationToken ct = default)
    {
        var products = await dbContext.Set<Product>()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Brand";
        ws.Cell(1, 3).Value = "Category";
        ws.Cell(1, 4).Value = "Description";
        ws.Cell(1, 5).Value = "ImageUrl";
        ws.Cell(1, 6).Value = "SKU";
        ws.Cell(1, 7).Value = "PrecioBase";
        ws.Cell(1, 8).Value = "Slug";
        ws.Cell(1, 9).Value = "VariantCount";

        var row = 2;
        foreach (var product in products)
        {
            ws.Cell(row, 1).Value = product.Name;
            ws.Cell(row, 2).Value = product.Brand.Nombre;
            ws.Cell(row, 3).Value = product.Category.Nombre;
            ws.Cell(row, 4).Value = product.Description ?? "";
            ws.Cell(row, 5).Value = product.ImageUrl;
            ws.Cell(row, 6).Value = product.Sku;
            ws.Cell(row, 7).Value = product.PrecioBase?.ToString("F2") ?? "";
            ws.Cell(row, 8).Value = product.Slug;
            ws.Cell(row, 9).Value = product.Variants.Count;
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportBrandsAsync(CancellationToken ct = default)
    {
        var brands = await dbContext.Set<Brand>()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nombre)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Brands");

        ws.Cell(1, 1).Value = "Nombre";
        ws.Cell(1, 2).Value = "Descripcion";
        ws.Cell(1, 3).Value = "Categoria";
        ws.Cell(1, 4).Value = "Origen";
        ws.Cell(1, 5).Value = "Especialidad";
        ws.Cell(1, 6).Value = "Destacada";
        ws.Cell(1, 7).Value = "Slug";

        var row = 2;
        foreach (var brand in brands)
        {
            ws.Cell(row, 1).Value = brand.Nombre;
            ws.Cell(row, 2).Value = brand.Descripcion ?? "";
            ws.Cell(row, 3).Value = brand.Categoria?.ToString() ?? "";
            ws.Cell(row, 4).Value = brand.Origen ?? "";
            ws.Cell(row, 5).Value = brand.Especialidad ?? "";
            ws.Cell(row, 6).Value = brand.Destacada ? "Yes" : "No";
            ws.Cell(row, 7).Value = brand.Slug;
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await dbContext.Set<Category>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Orden)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Categories");

        ws.Cell(1, 1).Value = "Nombre";
        ws.Cell(1, 2).Value = "Descripcion";
        ws.Cell(1, 3).Value = "Orden";
        ws.Cell(1, 4).Value = "Slug";

        var row = 2;
        foreach (var category in categories)
        {
            ws.Cell(row, 1).Value = category.Nombre;
            ws.Cell(row, 2).Value = category.Descripcion ?? "";
            ws.Cell(row, 3).Value = category.Orden;
            ws.Cell(row, 4).Value = category.Slug;
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public byte[] GetTemplate(string type)
    {
        using var workbook = new XLWorkbook();

        var (sheetName, headers, example) = type switch
        {
            "products" => ("Products",
                new[] { "Name", "Brand", "Category", "Description", "ImageUrl" },
                new[] { "Ceramic Coating Pro", "Gyeon", "Ceramicos", "Paint protection 3 years", "https://cdn.example.com/image.jpg" }),
            "brands" => ("Brands",
                new[] { "Nombre", "Descripcion", "Categoria", "Origen" },
                new[] { "Gyeon", "Premium products from Korea", "Ceramicos", "South Korea" }),
            "categories" => ("Categories",
                new[] { "Nombre", "Descripcion", "Orden" },
                new[] { "Ceramicos", "Ceramic coatings and sealants", "1" }),
            _ => throw new ArgumentException($"Unknown template type: {type}")
        };

        var ws = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(2, i + 1).Value = example[i];
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
