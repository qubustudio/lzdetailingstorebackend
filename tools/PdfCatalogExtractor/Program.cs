using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

var listaPreciosDir = @"D:\Carlos Quiroz\Documents\9.- LzDetailingStore\4.- Desarollo\4.2.- backend\lzdetailingstorebackend\ListaPrecios";
var outputDir = @"D:\Carlos Quiroz\Documents\9.- LzDetailingStore\4.- Desarollo\4.2.- backend\lzdetailingstorebackend\tools\PdfCatalogExtractor";

Console.OutputEncoding = Encoding.UTF8;

var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "DESENGRASANTES", "Desengrasantes" },
    { "LIMPIADORES ESPECIALIZADOS", "Limpiadores Especializados" },
    { "LIMPIADORES PARA INTERIOR", "Limpiadores Interior" },
    { "SHAMPOO", "Shampoos" },
    { "CERAS", "Ceras y Selladores" },
    { "PROTECC", "Plasticos y Proteccion" },
    { "COMPUESTOS PARA PULIDO", "Compuestos de Pulido" },
    { "PADS", "Pads y Esponjas" },
    { "ACCESORIOS", "Accesorios" },
    { "HERRAMIENTAS", "Herramientas" },
    { "VIDRIOS", "Limpiadores Vidrios" },
    { "ELIMINADOR", "Aromatizantes" },
    { "MOBILIARIO", "Mobiliario" },
    { "PULIDORA", "Pulidoras" },
};

var allProducts = new List<ProductData>();

var pdfFiles = Directory.GetFiles(listaPreciosDir, "*.pdf")
    .Where(f => new FileInfo(f).Length < 10_000_000)
    .OrderBy(f => new FileInfo(f).Length)
    .ToList();

Console.WriteLine($"Processing {pdfFiles.Count} PDF files...\n");

foreach (var pdfFile in pdfFiles)
{
    var fileName = Path.GetFileNameWithoutExtension(pdfFile);
    var brand = DetectBrand(fileName);

    Console.WriteLine($"Processing: {Path.GetFileName(pdfFile)} -> Brand: {brand}");

    try
    {
        using var reader = new PdfReader(pdfFile);
        using var document = new PdfDocument(reader);

        var fullText = new StringBuilder();
        for (int i = 1; i <= document.GetNumberOfPages(); i++)
        {
            var page = document.GetPage(i);
            var text = PdfTextExtractor.GetTextFromPage(page, new SimpleTextExtractionStrategy());
            fullText.AppendLine(text);
        }

        var products = ExtractProducts(fullText.ToString(), brand);
        allProducts.AddRange(products);
        Console.WriteLine($"  -> Extracted {products.Count} product lines");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
    }
}

Console.WriteLine($"\nTOTAL EXTRACTED: {allProducts.Count} product lines");

// Group into products with variants
var grouped = allProducts
    .GroupBy(p => (p.Brand, p.Name.ToLower().Trim()))
    .ToList();

Console.WriteLine($"UNIQUE PRODUCTS: {grouped.Count}");
Console.WriteLine($"TOTAL VARIANTS: {allProducts.Count}");
Console.WriteLine("\nGenerating SQL...");

var sql = GenerateSql(allProducts, grouped);
var sqlPath = Path.Combine(outputDir, "seed_catalog.sql");
File.WriteAllText(sqlPath, sql, Encoding.UTF8);
Console.WriteLine($"SQL written to: {sqlPath}");

var csv = GenerateCsv(allProducts);
var csvPath = Path.Combine(outputDir, "catalog_extracted.csv");
File.WriteAllText(csvPath, csv, Encoding.UTF8);
Console.WriteLine($"CSV written to: {csvPath}");

// --- Methods ---

string DetectBrand(string fileName)
{
    fileName = fileName.ToUpper();
    if (fileName.Contains("3D GLW")) return "3D GLW Series";
    if (fileName.Contains("3D CAR CARE") || (fileName.Contains("3D") && !fileName.Contains("GLW"))) return "3D Car Care";
    if (fileName.Contains("JESCAR")) return "Jescar";
    if (fileName.Contains("KERS")) return "Kers";
    if (fileName.Contains("MAJORWHEEL") || fileName.Contains("MAJOR")) return "Major Wheel";
    if (fileName.Contains("GYEON")) return "Gyeon";
    if (fileName.Contains("NANOLEX")) return "Nanolex";
    if (fileName.Contains("TONYIN")) return "Tonyin";
    return "Unknown";
}

List<ProductData> ExtractProducts(string text, string brand)
{
    var products = new List<ProductData>();
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Trim())
        .Where(l => l.Length > 0)
        .ToArray();

    var currentCategory = "General";
    var priceRegex = new Regex(@"\$\s*([\d,]+\.?\d*)", RegexOptions.Compiled);
    var skuRegex = new Regex(@"([A-Z0-9]{2,}[-][A-Z0-9]+[-]?[A-Z0-9]*|[0-9]{3}[A-Z]{2,3}[0-9]{2}|KRS-[A-Z0-9]+|MW[0-9]{3}-[0-9]{2}-[A-Z]{2}|J-[A-Z]+-?[A-Z0-9]*)", RegexOptions.Compiled);

    string? currentProductName = null;
    string? currentDescription = null;

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];

        // Category detection
        var catDetected = DetectCategory(line);
        if (catDetected != null) { currentCategory = catDetected; continue; }

        // Skip noise
        if (IsNoise(line)) continue;

        // Price line
        var priceMatch = priceRegex.Match(line);
        if (priceMatch.Success)
        {
            var priceStr = priceMatch.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) && price >= 100)
            {
                var skuMatch = skuRegex.Match(line);
                var sku = skuMatch.Success ? skuMatch.Value : "";
                var presentation = DetectPresentation(line);

                if (currentProductName == null)
                {
                    for (int j = i - 1; j >= Math.Max(0, i - 6); j--)
                    {
                        var prev = lines[j];
                        if (!priceRegex.IsMatch(prev) && !IsNoise(prev) && prev.Length > 3 && prev.Length < 60)
                        {
                            if (IsProductNameLine(prev))
                            {
                                currentProductName = CleanName(prev);
                                break;
                            }
                        }
                    }
                }

                if (currentProductName != null)
                {
                    products.Add(new ProductData
                    {
                        Brand = brand,
                        Name = currentProductName,
                        Category = currentCategory,
                        Sku = sku,
                        Price = price,
                        Presentation = presentation,
                        Description = (currentDescription ?? "").Trim()
                    });
                }

                // Keep product name if next line also has price (variants)
                if (i + 1 >= lines.Length || !priceRegex.IsMatch(lines[i + 1]))
                {
                    currentProductName = null;
                    currentDescription = null;
                }
            }
        }
        else if (IsProductNameLine(line) && line.Length < 50)
        {
            currentProductName = CleanName(line);
            currentDescription = null;
        }
        else if (currentProductName != null && line.Length > 20 && line.Length < 300)
        {
            currentDescription = ((currentDescription ?? "") + " " + line).Trim();
            if (currentDescription.Length > 400) currentDescription = currentDescription[..400];
        }
    }

    return products;
}

string? DetectCategory(string line)
{
    var upper = line.ToUpper();
    foreach (var kvp in categoryMap)
    {
        if (upper.Contains(kvp.Key.ToUpper()) && (line.Length < 60 || upper.StartsWith(kvp.Key.ToUpper())))
            return kvp.Value;
    }
    return null;
}

bool IsNoise(string line)
{
    var u = line.ToUpper();
    return u.Contains("LISTA DE PRECIOS") || u.Contains("PRECIOS CON IVA") ||
           u.Contains("NO INCLUIDO") || u.Contains("LZSTOREMX") || u.Contains("@GMAIL") ||
           u.Contains("5592330524") || u.Contains("5580364173") || u.Contains("5579285830") ||
           u.Contains("WHATSAPP") || u.Contains("PEDIDO ESPECIAL") ||
           u.Contains("E-MAIL") || (u.Contains("TEL.") && line.Length < 15) ||
           u.Contains("# PARTE") || u.Contains("PRECIO NETO") ||
           Regex.IsMatch(line, @"^(ENERO|FEBRERO|MARZO|ABRIL|MAYO|JUNIO|JULIO|AGOSTO|SEPTIEMBRE|OCTUBRE|NOVIEMBRE|DICIEMBRE)\s*\d{4}$", RegexOptions.IgnoreCase) ||
           Regex.IsMatch(line, @"^\d{4}$") ||
           (u == "TIENDA" || u == "CURSOS" || u == "DIL" || u == "UC");
}

bool IsProductNameLine(string line)
{
    if (line.Length < 3 || line.Length > 55) return false;
    // All caps or starts with uppercase and has no $ sign
    if (line.Contains("$")) return false;
    if (line == line.ToUpper()) return true;
    if (char.IsUpper(line[0]) && !line.Contains("PRECIO") && !line.Contains("DESCRIPCI")) return true;
    return false;
}

string CleanName(string name)
{
    name = name.Trim();
    name = Regex.Replace(name, @"\d{10}$", "").Trim();
    if (name == name.ToUpper() && name.Length > 3)
        name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLower());
    return name;
}

string DetectPresentation(string line)
{
    var u = line.ToUpper();
    if (u.Contains("5 GALONES") || u.Contains("CUBETA 19")) return "5 Galones (19 L)";
    if (u.Contains("GALÓN") || u.Contains("GALON") || u.Contains("GAL")) return "Galon (3.785 L)";
    if (u.Contains("64OZ") || u.Contains("64 OZ")) return "64 Oz (1.893 L)";
    if (u.Contains("32OZ") || u.Contains("32 OZ") || u.Contains("32Oz")) return "32 Oz (946 mL)";
    if (u.Contains("22OZ") || u.Contains("22 OZ")) return "22 Oz (650 mL)";
    if (u.Contains("16OZ") || u.Contains("16 OZ") || u.Contains("16Oz")) return "16 Oz (473 mL)";
    if (u.Contains("08OZ") || u.Contains("8 OZ") || u.Contains("8OZ")) return "8 Oz (237 mL)";
    if (u.Contains("11 ONZAS")) return "11 Oz";
    if (u.Contains("05 OZ") || u.Contains("5 OZ")) return "5 Oz";
    if (u.Contains("30 ML") || u.Contains("30ML")) return "30 mL";
    if (u.Contains("PZA") || u.Contains("PIEZA")) return "Pieza";
    if (Regex.IsMatch(u, @"\d+\s*CM")) return "Pieza";
    return "Unidad";
}

string GenerateSql(List<ProductData> products, List<IGrouping<(string Brand, string), ProductData>> grouped)
{
    var sb = new StringBuilder();
    sb.AppendLine("-- LZ Detailing Store - Catalog Seed Data");
    sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
    sb.AppendLine($"-- Products: {grouped.Count}, Variants: {products.Count}");
    sb.AppendLine();

    // Categories
    var categories = products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
    sb.AppendLine("-- CATEGORIES");
    int catOrder = 0;
    foreach (var cat in categories)
    {
        catOrder++;
        sb.AppendLine($"INSERT INTO category (id, nombre, slug, is_active, orden, created_at, updated_at) VALUES (gen_random_uuid(), '{Esc(cat)}', '{Slug(cat)}', true, {catOrder}, NOW(), NOW()) ON CONFLICT (slug) DO NOTHING;");
    }
    sb.AppendLine();

    // Brands
    var brands = products.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList();
    sb.AppendLine("-- BRANDS");
    foreach (var b in brands)
    {
        sb.AppendLine($"INSERT INTO brand (id, nombre, slug, is_active, destacada, created_at, updated_at) VALUES (gen_random_uuid(), '{Esc(b)}', '{Slug(b)}', true, false, NOW(), NOW()) ON CONFLICT (slug) DO NOTHING;");
    }
    sb.AppendLine();

    // Products + Variants
    sb.AppendLine("-- PRODUCTS + VARIANTS");
    int pNum = 0;
    var rng = new Random(42);
    foreach (var group in grouped)
    {
        pNum++;
        var first = group.First();
        var brandSlug = Slug(first.Brand);
        var catSlug = Slug(first.Category);
        var prodSlug = Slug($"{first.Brand} {first.Name}");
        if (prodSlug.Length > 160) prodSlug = prodSlug[..160];
        prodSlug = $"{prodSlug}-{pNum}"; // ensure uniqueness
        var baseSku = $"LZ-{pNum:D4}"; // always unique product-level SKU
        var minPrice = group.Min(v => v.Price);
        var desc = first.Description.Length > 10 ? Esc(first.Description[..Math.Min(400, first.Description.Length)]) : "";

        sb.AppendLine($"DO $$ DECLARE pid uuid := gen_random_uuid(); bid uuid; cid uuid; BEGIN");
        sb.AppendLine($"  SELECT id INTO bid FROM brand WHERE slug='{brandSlug}';");
        sb.AppendLine($"  SELECT id INTO cid FROM category WHERE slug='{catSlug}';");
        sb.AppendLine($"  INSERT INTO product (id,brand_id,category_id,sku,name,slug,description,precio_base,image_url,is_active,rating,review_count,created_at,updated_at)");
        sb.AppendLine($"  VALUES(pid,bid,cid,'{Esc(baseSku)}','{Esc(first.Name)}','{prodSlug}','{desc}',{minPrice.ToString(CultureInfo.InvariantCulture)},'https://placehold.co/400x400/1a1a2e/ffffff?text={Uri.EscapeDataString(first.Name[..Math.Min(12, first.Name.Length)])}',true,0,0,NOW(),NOW()) ON CONFLICT(sku) DO NOTHING;");

        int vNum = 0;
        foreach (var v in group.OrderBy(x => x.Price))
        {
            vNum++;
            var vSku = $"{baseSku}-V{vNum}"; // always unique variant SKU
            var (sz, unit) = GetSize(v.Presentation);
            var stock = rng.Next(5, 40);
            var costo = (v.Price * 0.55m).ToString("F2", CultureInfo.InvariantCulture);

            sb.AppendLine($"  INSERT INTO product_variant (id,product_id,sku,nombre,precio_venta,precio_costo,size_value,size_unit,stock,is_active,sort_order,created_at,updated_at)");
            sb.AppendLine($"  VALUES(gen_random_uuid(),pid,'{Esc(vSku)}','{Esc(v.Presentation)}',{v.Price.ToString(CultureInfo.InvariantCulture)},{costo},{sz.ToString(CultureInfo.InvariantCulture)},'{unit}',{stock},true,{vNum},NOW(),NOW()) ON CONFLICT(sku) DO NOTHING;");
        }
        sb.AppendLine("END $$;");
        sb.AppendLine();
    }

    sb.AppendLine($"-- Done: {categories.Count} categories, {brands.Count} brands, {grouped.Count} products, {products.Count} variants");
    return sb.ToString();
}

string GenerateCsv(List<ProductData> products)
{
    var sb = new StringBuilder();
    sb.AppendLine("Brand,Name,Category,SKU,Price,Presentation");
    foreach (var p in products)
        sb.AppendLine($"\"{p.Brand}\",\"{p.Name}\",\"{p.Category}\",\"{p.Sku}\",{p.Price},\"{p.Presentation}\"");
    return sb.ToString();
}

string Slug(string t)
{
    t = t.ToLower().Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u").Replace("ñ","n");
    t = Regex.Replace(t, @"[^a-z0-9\s-]", "");
    t = Regex.Replace(t, @"\s+", "-");
    return Regex.Replace(t, @"-+", "-").Trim('-');
}

string Esc(string t) => t.Replace("'", "''").Replace("\r", "").Replace("\n", " ");

(decimal, string) GetSize(string p)
{
    var u = p.ToUpper();
    if (u.Contains("19 L") || u.Contains("5 GALONES")) return (19m, "litros");
    if (u.Contains("3.785") || u.Contains("GALON")) return (3.785m, "litros");
    if (u.Contains("1.893") || u.Contains("64 OZ")) return (1893m, "ml");
    if (u.Contains("946") || u.Contains("32 OZ")) return (946m, "ml");
    if (u.Contains("650") || u.Contains("22 OZ")) return (650m, "ml");
    if (u.Contains("473") || u.Contains("16 OZ")) return (473m, "ml");
    if (u.Contains("237") || u.Contains("8 OZ")) return (237m, "ml");
    if (u.Contains("30 ML")) return (30m, "ml");
    return (1m, "pza");
}

record ProductData
{
    public string Brand { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Sku { get; init; } = "";
    public decimal Price { get; init; }
    public string Presentation { get; init; } = "";
    public string Description { get; init; } = "";
}
