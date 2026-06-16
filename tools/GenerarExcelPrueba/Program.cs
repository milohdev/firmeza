using OfficeOpenXml;

ExcelPackage.License.SetNonCommercialPersonal("Firmeza");

using var package = new ExcelPackage();
var ws = package.Workbook.Worksheets.Add("Datos");

// ── Encabezados ──────────────────────────────────────────────────────────────
string[] headers =
[
    "NombreProducto", "DescripcionProducto", "PrecioProducto", "StockProducto", "CategoriaProducto", "UnidadMedida",
    "NombreCliente", "Documento", "EmailCliente", "TelefonoCliente", "DireccionCliente", "Edad",
    "DocumentoCliente", "FechaVenta", "NombreProductoVenta", "CantidadVenta", "PrecioUnitarioVenta"
];

// Índices de columna (1-based) por nombre de columna
var col = new Dictionary<string, int>();
for (int i = 0; i < headers.Length; i++)
{
    ws.Cells[1, i + 1].Value = headers[i];
    col[headers[i]] = i + 1;
}

// Estilo de encabezado
using (var rng = ws.Cells[1, 1, 1, headers.Length])
{
    rng.Style.Font.Bold = true;
    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
    rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
}

void Set(int row, string colName, object value)
{
    if (col.TryGetValue(colName, out int c))
        ws.Cells[row, c].Value = value;
}

// ── Fila 2: Producto válido 1 ─────────────────────────────────────────────
Set(2, "NombreProducto",    "Cemento Portland");
Set(2, "DescripcionProducto", "Cemento para construcción general, bolsa 50 kg");
Set(2, "PrecioProducto",    5000m);
Set(2, "StockProducto",     100);
Set(2, "CategoriaProducto", "Cementos");
Set(2, "UnidadMedida",      "Bolsa");

// ── Fila 3: Producto válido 2 ─────────────────────────────────────────────
Set(3, "NombreProducto",    "Arena Gruesa");
Set(3, "DescripcionProducto", "Arena gruesa para mezclas");
Set(3, "PrecioProducto",    2000m);
Set(3, "StockProducto",     500);
Set(3, "CategoriaProducto", "Áridos");
Set(3, "UnidadMedida",      "m3");

// ── Fila 4: Producto con precio inválido (error esperado) ─────────────────
Set(4, "NombreProducto",    "Ladrillo Hueco");
Set(4, "DescripcionProducto", "Ladrillo hueco 18x19x33");
Set(4, "PrecioProducto",    -1);   // PRECIO INVÁLIDO
Set(4, "StockProducto",     1000);
Set(4, "CategoriaProducto", "Ladrillos");
Set(4, "UnidadMedida",      "Unidad");

// ── Fila 5: Cliente válido 1 ──────────────────────────────────────────────
Set(5, "NombreCliente",   "Juan Pérez");
Set(5, "Documento",       "12345678");
Set(5, "EmailCliente",    "juan.perez@ejemplo.com");
Set(5, "TelefonoCliente", "11-5555-1234");
Set(5, "DireccionCliente","Av. Siempre Viva 123, Springfield");
Set(5, "Edad",             35);

// ── Fila 6: Cliente válido 2 ──────────────────────────────────────────────
Set(6, "NombreCliente",   "María García");
Set(6, "Documento",       "87654321");
Set(6, "EmailCliente",    "maria.garcia@ejemplo.com");
Set(6, "TelefonoCliente", "11-5555-5678");
Set(6, "DireccionCliente","Calle Falsa 742");
Set(6, "Edad",             28);

// ── Fila 7: Cliente con edad inválida (error esperado) ───────────────────
Set(7, "NombreCliente",   "Pedro Inválido");
Set(7, "Documento",       "11111111");
Set(7, "EmailCliente",    "pedro@ejemplo.com");
Set(7, "Edad",             200);   // EDAD INVÁLIDA

// ── Fila 8: Venta 1 – Juan Pérez compra Cemento ──────────────────────────
var hoy = DateTime.Today;
Set(8, "DocumentoCliente",    "12345678");
Set(8, "FechaVenta",           hoy);
Set(8, "NombreProductoVenta", "Cemento Portland");
Set(8, "CantidadVenta",        5);
Set(8, "PrecioUnitarioVenta",  5000m);

// ── Fila 9: Venta 2 – María García compra Arena ──────────────────────────
Set(9, "DocumentoCliente",    "87654321");
Set(9, "FechaVenta",           hoy);
Set(9, "NombreProductoVenta", "Arena Gruesa");
Set(9, "CantidadVenta",        10);
Set(9, "PrecioUnitarioVenta",  2000m);

// Formato de fecha para columna FechaVenta
ws.Column(col["FechaVenta"]).Style.Numberformat.Format = "dd/mm/yyyy";

// Auto-ajustar anchos
ws.Cells[ws.Dimension.Address].AutoFitColumns();

var salida = Path.Combine(Directory.GetCurrentDirectory(), "datos_prueba.xlsx");
package.SaveAs(new FileInfo(salida));
Console.WriteLine($"Archivo generado: {salida}");
Console.WriteLine();
Console.WriteLine("Contenido del archivo:");
Console.WriteLine("  Fila 2: Producto válido  - Cemento Portland (precio: 5000, stock: 100)");
Console.WriteLine("  Fila 3: Producto válido  - Arena Gruesa (precio: 2000, stock: 500)");
Console.WriteLine("  Fila 4: Producto ERROR   - Ladrillo Hueco (precio: -1 → inválido)");
Console.WriteLine("  Fila 5: Cliente válido   - Juan Pérez (doc: 12345678, edad: 35)");
Console.WriteLine("  Fila 6: Cliente válido   - María García (doc: 87654321, edad: 28)");
Console.WriteLine("  Fila 7: Cliente ERROR    - Pedro Inválido (edad: 200 → inválida)");
Console.WriteLine("  Fila 8: Venta            - Juan Pérez compra 5x Cemento = $25.000");
Console.WriteLine("  Fila 9: Venta            - María García compra 10x Arena = $20.000");
