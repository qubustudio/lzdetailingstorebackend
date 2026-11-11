using ClosedXML.Excel;

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "seed-data");
Directory.CreateDirectory(outputDir);

// === BRANDS ===
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Brands");
    ws.Cell(1, 1).Value = "Nombre";
    ws.Cell(1, 2).Value = "Descripcion";
    ws.Cell(1, 3).Value = "Categoria";
    ws.Cell(1, 4).Value = "Origen";

    var brands = new (string Nombre, string Desc, string Cat, string Origen)[]
    {
        ("3D Car Care", "Productos profesionales de detailing americanos. Linea completa de pulidos, ceras y productos de mantenimiento.", "Pulido", "Estados Unidos"),
        ("3D GLW Series", "Linea premium de 3D enfocada en acabados de alto brillo y proteccion avanzada.", "Pulido", "Estados Unidos"),
        ("Gyeon", "Marca coreana lider en recubrimientos ceramicos y productos de cuidado automotriz de alta gama.", "Ceramicos", "Corea del Sur"),
        ("Jescar", "Productos de pulido y correccion de pintura de grado profesional.", "Pulido", "Estados Unidos"),
        ("Nanolex", "Tecnologia alemana en proteccion de superficies y recubrimientos nanotecnologicos.", "Ceramicos", "Alemania"),
        ("Kers", "Marca brasilena de productos quimicos para detailing profesional.", "Lavado", "Brasil"),
        ("Majorwheel", "Herramientas y accesorios profesionales para detailing automotriz.", "Herramientas", "China"),
        ("Tonyin", "Marca especializada en recubrimientos nanoceramicos, PPF, polarizado y productos de cuidado automotriz.", "Ceramicos", "China"),
    };

    for (var i = 0; i < brands.Length; i++)
    {
        ws.Cell(i + 2, 1).Value = brands[i].Nombre;
        ws.Cell(i + 2, 2).Value = brands[i].Desc;
        ws.Cell(i + 2, 3).Value = brands[i].Cat;
        ws.Cell(i + 2, 4).Value = brands[i].Origen;
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(Path.Combine(outputDir, "import-brands.xlsx"));
}

// === CATEGORIES (17 specific) ===
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Categories");
    ws.Cell(1, 1).Value = "Nombre";
    ws.Cell(1, 2).Value = "Descripcion";
    ws.Cell(1, 3).Value = "Orden";

    var categories = new (string Nombre, string Desc, int Orden)[]
    {
        ("Recubrimientos Ceramicos", "Coatings ceramicos profesionales de larga duracion (SiO2, grafeno)", 1),
        ("Selladores y Ceras", "Selladores sinteticos, ceras naturales y spray sealants de proteccion", 2),
        ("Compuestos de Corte", "Compuestos abrasivos para remocion de defectos severos en pintura", 3),
        ("Pulidores de Acabado", "Pulidores finos para brillo, claridad y eliminacion de hologramas", 4),
        ("Shampoos", "Shampoos automotrices pH neutro, ceramicos y de mantenimiento", 5),
        ("Descontaminantes", "Iron removers, clay bars, fallout removers y descontaminantes quimicos", 6),
        ("APC y Limpiadores", "Limpiadores multiproposito, degreasers y limpiadores especializados", 7),
        ("Cuidado de Interior", "Productos para piel, vinil, plasticos, telas y tablero", 8),
        ("Llantas y Rines", "Tire dressing, wheel cleaners, renovadores y protectores de llantas", 9),
        ("Pulidoras", "Pulidoras DA, rotativas y mini polishers profesionales", 10),
        ("Pads de Pulido", "Pads de espuma, microfibra y lana para correccion y acabado", 11),
        ("Microfibras y Toallas", "Toallas de secado, buffing, glass y aplicacion", 12),
        ("Brochas y Aplicadores", "Brochas de detailing, aplicadores de ceramico y esponjas", 13),
        ("Vidrios", "Limpiadores de vidrio, repelentes de agua y tratamientos para parabrisas", 14),
        ("PPF y Polarizado", "Peliculas de proteccion de pintura y window tint", 15),
        ("Aerosoles", "Productos en aerosol: quick detailers, ambientadores, lubricantes", 16),
        ("Aditivos", "Aditivos para motor, aceite y combustible", 17),
    };

    for (var i = 0; i < categories.Length; i++)
    {
        ws.Cell(i + 2, 1).Value = categories[i].Nombre;
        ws.Cell(i + 2, 2).Value = categories[i].Desc;
        ws.Cell(i + 2, 3).Value = categories[i].Orden;
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(Path.Combine(outputDir, "import-categories.xlsx"));
}

// === PRODUCTS (56) ===
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Products");
    ws.Cell(1, 1).Value = "Name";
    ws.Cell(1, 2).Value = "Brand";
    ws.Cell(1, 3).Value = "Category";
    ws.Cell(1, 4).Value = "Description";
    ws.Cell(1, 5).Value = "ImageUrl";

    var products = new (string Name, string Brand, string Category, string Desc)[]
    {
        ("3D Speed", "3D Car Care", "Compuestos de Corte", "Compuesto de pulido de corte rapido para remocion de defectos severos"),
        ("3D One", "3D Car Care", "Pulidores de Acabado", "Compuesto todo en uno: corta, pule y protege en un solo paso"),
        ("3D Correction Gloss Enhancer", "3D Car Care", "Pulidores de Acabado", "Potenciador de brillo y correccion ligera"),
        ("3D HD Polish", "3D Car Care", "Pulidores de Acabado", "Pulidor de acabado fino para maxima claridad"),
        ("3D Wash N Wax", "3D Car Care", "Shampoos", "Shampoo con cera para lavado y proteccion simultanea"),
        ("3D Bead It Up", "3D Car Care", "Selladores y Ceras", "Sellador ceramico en spray de mantenimiento rapido"),
        ("3D Cherry Scent APC", "3D Car Care", "APC y Limpiadores", "Limpiador multiproposito con aroma a cereza"),
        ("3D LVP Conditioner", "3D Car Care", "Cuidado de Interior", "Acondicionador para piel, vinil y plasticos"),
        ("3D Tire Shine", "3D Car Care", "Llantas y Rines", "Brillo para llantas de larga duracion"),
        ("3D Iron Remover", "3D Car Care", "Descontaminantes", "Descontaminante ferrico reactivo para pintura y rines"),
        ("3D GLW SiO2 Ceramic Coating", "3D GLW Series", "Recubrimientos Ceramicos", "Recubrimiento ceramico SiO2 de alta durabilidad"),
        ("3D GLW Ceramic Wash", "3D GLW Series", "Shampoos", "Shampoo ceramico pH neutro para mantenimiento de coatings"),
        ("3D GLW Ceramic Spray", "3D GLW Series", "Selladores y Ceras", "Sellador ceramico en spray para proteccion rapida"),
        ("3D GLW Finishing Polish", "3D GLW Series", "Pulidores de Acabado", "Pulidor de acabado de la linea premium GLW"),
        ("Gyeon Q2 EVO Mohs", "Gyeon", "Recubrimientos Ceramicos", "Recubrimiento ceramico profesional 2 capas, durabilidad 2+ anios"),
        ("Gyeon Q2 EVO Pure", "Gyeon", "Recubrimientos Ceramicos", "Recubrimiento ceramico de una capa, facil aplicacion"),
        ("Gyeon Q2M Bathe+", "Gyeon", "Shampoos", "Shampoo de mantenimiento con SiO2 integrado"),
        ("Gyeon Q2M Compound+", "Gyeon", "Compuestos de Corte", "Compuesto de corte agresivo para defectos severos"),
        ("Gyeon Q2M Polish", "Gyeon", "Pulidores de Acabado", "Pulidor de acabado medio para eliminacion de marcas"),
        ("Gyeon Q2M Iron", "Gyeon", "Descontaminantes", "Descontaminante ferrico pH balanceado"),
        ("Gyeon Q2M Foam", "Gyeon", "Shampoos", "Pre-lavado espumante de alta lubricacion"),
        ("Gyeon Q2M Tire", "Gyeon", "Llantas y Rines", "Acondicionador de llantas con acabado satinado"),
        ("Gyeon Q2M Leather Cleaner", "Gyeon", "Cuidado de Interior", "Limpiador de piel suave pH neutro"),
        ("Gyeon Q2M Glass", "Gyeon", "Vidrios", "Limpiador de vidrios sin residuos"),
        ("Jescar Micro Finishing Polish", "Jescar", "Pulidores de Acabado", "Pulidor de micro acabado para perfeccion de brillo"),
        ("Jescar Correcting Compound", "Jescar", "Compuestos de Corte", "Compuesto de correccion para defectos moderados"),
        ("Jescar Power Lock Plus", "Jescar", "Selladores y Ceras", "Sellador sintetico de polimeros de larga duracion"),
        ("Jescar All-In-One", "Jescar", "Pulidores de Acabado", "Pulidor todo en uno: corrige y sella"),
        ("Nanolex Si3D", "Nanolex", "Recubrimientos Ceramicos", "Recubrimiento ceramico profesional 3 capas"),
        ("Nanolex Si3D Lite", "Nanolex", "Recubrimientos Ceramicos", "Recubrimiento ceramico de facil aplicacion para entusiastas"),
        ("Nanolex EX Decon", "Nanolex", "Descontaminantes", "Descontaminante acido para remocion de depositos minerales"),
        ("Nanolex Pure Shampoo", "Nanolex", "Shampoos", "Shampoo pH neutro para superficies protegidas"),
        ("Nanolex Matte Finalizer", "Nanolex", "Selladores y Ceras", "Proteccion para acabados mate y satin"),
        ("Nanolex Glass Sealant", "Nanolex", "Vidrios", "Sellador de vidrios repelente de agua"),
        ("Kers Iron Decon", "Kers", "Descontaminantes", "Descontaminante ferrico de accion rapida"),
        ("Kers Nano Shampoo", "Kers", "Shampoos", "Shampoo con nanotecnologia pH neutro"),
        ("Kers APC", "Kers", "APC y Limpiadores", "Limpiador multiproposito concentrado"),
        ("Kers Tire Black", "Kers", "Llantas y Rines", "Renovador de llantas acabado negro profundo"),
        ("Kers Ceramic Coating", "Kers", "Recubrimientos Ceramicos", "Recubrimiento ceramico brasileno de alta dureza"),
        ("Majorwheel DA Polisher 15mm", "Majorwheel", "Pulidoras", "Pulidora dual action orbita 15mm para correccion"),
        ("Majorwheel DA Polisher 21mm", "Majorwheel", "Pulidoras", "Pulidora dual action orbita 21mm para acabado"),
        ("Majorwheel Rotary Polisher", "Majorwheel", "Pulidoras", "Pulidora rotativa profesional de alta potencia"),
        ("Majorwheel Foam Pad Cutting 6in", "Majorwheel", "Pads de Pulido", "Pad de espuma de corte 6 pulgadas"),
        ("Majorwheel Foam Pad Polishing 6in", "Majorwheel", "Pads de Pulido", "Pad de espuma de pulido 6 pulgadas"),
        ("Majorwheel Foam Pad Finishing 6in", "Majorwheel", "Pads de Pulido", "Pad de espuma de acabado 6 pulgadas"),
        ("Majorwheel Microfiber Towel 40x40", "Majorwheel", "Microfibras y Toallas", "Toalla de microfibra 40x40cm 400gsm"),
        ("Majorwheel Detail Brush Set", "Majorwheel", "Brochas y Aplicadores", "Set de brochas de detailing 5 piezas"),
        ("Tonyin Nano Ceramic Coating 9H", "Tonyin", "Recubrimientos Ceramicos", "Recubrimiento nanoceramico 9H de alta dureza y brillo"),
        ("Tonyin Graphene Coating", "Tonyin", "Recubrimientos Ceramicos", "Recubrimiento de grafeno de ultra durabilidad"),
        ("Tonyin Clay Bar Kit", "Tonyin", "Descontaminantes", "Kit de clay bar para descontaminacion de pintura"),
        ("Tonyin Carnauba Wax", "Tonyin", "Selladores y Ceras", "Cera de carnauba premium alto brillo"),
        ("Tonyin Cutting Compound", "Tonyin", "Compuestos de Corte", "Compuesto de corte para remocion de rayones"),
        ("Tonyin Fine Polish", "Tonyin", "Pulidores de Acabado", "Pulidor fino para acabado espejo"),
        ("Tonyin Interior Cleaner", "Tonyin", "Cuidado de Interior", "Limpiador de interiores multiusos"),
        ("Tonyin Wheel Cleaner", "Tonyin", "Llantas y Rines", "Limpiador de rines y llantas desincrustante"),
        ("Tonyin Quick Detailer Spray", "Tonyin", "Aerosoles", "Quick detailer en aerosol para brillo instantaneo"),
        ("Tonyin PPF Protection Film", "Tonyin", "PPF y Polarizado", "Pelicula de proteccion de pintura transparente"),
    };

    for (var i = 0; i < products.Length; i++)
    {
        ws.Cell(i + 2, 1).Value = products[i].Name;
        ws.Cell(i + 2, 2).Value = products[i].Brand;
        ws.Cell(i + 2, 3).Value = products[i].Category;
        ws.Cell(i + 2, 4).Value = products[i].Desc;
        ws.Cell(i + 2, 5).Value = "";
    }

    ws.Columns().AdjustToContents();
    wb.SaveAs(Path.Combine(outputDir, "import-products.xlsx"));
}

Console.WriteLine("Excel files generated in: " + outputDir);
Console.WriteLine("- import-brands.xlsx (8 brands)");
Console.WriteLine("- import-categories.xlsx (17 categories)");
Console.WriteLine("- import-products.xlsx (56 products)");
