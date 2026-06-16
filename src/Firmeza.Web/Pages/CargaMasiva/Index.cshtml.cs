using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.Validation;
using Firmeza.Web.ViewModels;

namespace Firmeza.Web.Pages.CargaMasiva
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public ResultadoCarga? Resultado { get; set; }
        public string? ErrorGeneral { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Archivo == null || Archivo.Length == 0)
            {
                ErrorGeneral = "Debe seleccionar un archivo .xlsx.";
                return Page();
            }

            if (!Archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ErrorGeneral = "El archivo debe tener extensión .xlsx.";
                return Page();
            }

            ExcelPackage.License.SetNonCommercialPersonal("Firmeza");

            using var stream = Archivo.OpenReadStream();
            ExcelPackage package;
            try
            {
                package = new ExcelPackage(stream);
            }
            catch (Exception ex)
            {
                ErrorGeneral = $"No se pudo leer el archivo: {ex.Message}";
                return Page();
            }

            using (package)
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null)
                {
                    ErrorGeneral = "El archivo está vacío o no contiene datos.";
                    return Page();
                }

                Dictionary<string, int> headers;
                try
                {
                    headers = LeerHeaders(ws);
                }
                catch (Exception ex)
                {
                    ErrorGeneral = $"Error al leer encabezados: {ex.Message}";
                    return Page();
                }

                var resultado = new ResultadoCarga();
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await ProcesarHoja(ws, headers, resultado);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ErrorGeneral = $"Error crítico al guardar los datos: {ex.Message}";
                    return Page();
                }

                Resultado = resultado;
            }

            return Page();
        }

        private async Task ProcesarHoja(
            ExcelWorksheet ws,
            Dictionary<string, int> headers,
            ResultadoCarga resultado)
        {
            int totalRows = ws.Dimension.Rows;
            int totalCols = ws.Dimension.Columns;

            // Pre-load existing products and clients (first match wins on duplicates)
            var productosDict = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in await _context.Productos.ToListAsync())
                productosDict.TryAdd(p.Nombre.Trim(), p);

            var clientesDict = new Dictionary<string, Client>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in await _context.Clientes.ToListAsync())
                clientesDict.TryAdd(c.Documento.Trim(), c);

            // Two-pass: first products + clients so they're available for sale references
            for (int row = 2; row <= totalRows; row++)
            {
                if (EsFilaVacia(ws, row, totalCols)) continue;
                string Get(string col) => GetCellStr(ws, row, headers, col);

                var nomProd = Get("NombreProducto");
                if (!string.IsNullOrWhiteSpace(nomProd))
                    ProcesarProducto(row, nomProd, Get, productosDict, resultado);

                var doc = Get("Documento");
                if (!string.IsNullOrWhiteSpace(doc))
                    ProcesarCliente(row, doc, Get, clientesDict, resultado);
            }

            // Second pass: ventas/detalles
            var ventasDict = new Dictionary<(string, DateTime), Sale>();
            for (int row = 2; row <= totalRows; row++)
            {
                if (EsFilaVacia(ws, row, totalCols)) continue;
                string Get(string col) => GetCellStr(ws, row, headers, col);

                var fechaStr = Get("FechaVenta");
                if (!string.IsNullOrWhiteSpace(fechaStr))
                    ProcesarVenta(row, fechaStr, Get, productosDict, clientesDict, ventasDict, resultado);
            }
        }

        private void ProcesarProducto(
            int row, string nombreProducto,
            Func<string, string> get,
            Dictionary<string, Product> dict,
            ResultadoCarga resultado)
        {
            var precioStr = get("PrecioProducto");
            var stockStr = get("StockProducto");

            if (!decimal.TryParse(precioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || !ValidacionCargaMasiva.PrecioEsValido(precio))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Producto '{nombreProducto}': precio inválido '{precioStr}' (debe ser > 0)."));
                return;
            }

            if (!ValidacionCargaMasiva.TryParseEntero(stockStr, out int stock) || stock < 0)
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Producto '{nombreProducto}': stock inválido '{stockStr}' (debe ser >= 0)."));
                return;
            }

            var desc = get("DescripcionProducto");
            var cat = get("CategoriaProducto");
            var uni = get("UnidadMedida");
            var key = nombreProducto.Trim();

            if (dict.TryGetValue(key, out var existing))
            {
                existing.Precio = precio;
                existing.Stock = stock;
                if (!string.IsNullOrWhiteSpace(desc)) existing.Descripcion = desc;
                if (!string.IsNullOrWhiteSpace(cat)) existing.Categoria = cat;
                if (!string.IsNullOrWhiteSpace(uni)) existing.UnidadMedida = uni;
                resultado.ProductosActualizados++;
            }
            else
            {
                var p = new Product
                {
                    Nombre = key,
                    Descripcion = Nulo(desc),
                    Precio = precio,
                    Stock = stock,
                    Categoria = Nulo(cat),
                    UnidadMedida = string.IsNullOrWhiteSpace(uni) ? "Unidad" : uni
                };
                _context.Productos.Add(p);
                dict[key] = p;
                resultado.ProductosInsertados++;
            }
        }

        private void ProcesarCliente(
            int row, string documento,
            Func<string, string> get,
            Dictionary<string, Client> dict,
            ResultadoCarga resultado)
        {
            var nombre = get("NombreCliente");
            var edadStr = get("Edad");
            var email = get("EmailCliente");

            if (string.IsNullOrWhiteSpace(nombre))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Cliente documento '{documento}': NombreCliente es obligatorio."));
                return;
            }

            if (!ValidacionCargaMasiva.TryParseEntero(edadStr, out int edad) || !ValidacionCargaMasiva.EdadEsValida(edad))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Cliente '{nombre}': edad inválida '{edadStr}' (debe ser un entero entre 0 y 120)."));
                return;
            }

            if (!string.IsNullOrWhiteSpace(email) && !ValidacionCargaMasiva.EsEmailValido(email))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Cliente '{nombre}': correo inválido '{email}'."));
                return;
            }

            var tel = get("TelefonoCliente");
            var dir = get("DireccionCliente");
            var key = documento.Trim();

            if (dict.TryGetValue(key, out var existing))
            {
                existing.Nombre = nombre.Trim();
                existing.Edad = edad;
                if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
                if (!string.IsNullOrWhiteSpace(tel)) existing.Telefono = tel;
                if (!string.IsNullOrWhiteSpace(dir)) existing.Direccion = dir;
                resultado.ClientesActualizados++;
            }
            else
            {
                var c = new Client
                {
                    Nombre = nombre.Trim(),
                    Documento = key,
                    Edad = edad,
                    Email = Nulo(email),
                    Telefono = Nulo(tel),
                    Direccion = Nulo(dir)
                };
                _context.Clientes.Add(c);
                dict[key] = c;
                resultado.ClientesInsertados++;
            }
        }

        private void ProcesarVenta(
            int row, string fechaStr,
            Func<string, string> get,
            Dictionary<string, Product> productosDict,
            Dictionary<string, Client> clientesDict,
            Dictionary<(string, DateTime), Sale> ventasDict,
            ResultadoCarga resultado)
        {
            if (!TryParseDate(fechaStr, out DateTime fechaVenta))
            {
                resultado.Errores.Add(new ErrorFila(row, $"Venta: fecha inválida '{fechaStr}'."));
                return;
            }

            var docCliente = get("DocumentoCliente").Trim();
            var nomProd = get("NombreProductoVenta").Trim();
            var cantStr = get("CantidadVenta");
            var precStr = get("PrecioUnitarioVenta");

            if (!clientesDict.TryGetValue(docCliente, out var cliente))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Venta: cliente con documento '{docCliente}' no encontrado."));
                return;
            }

            if (!productosDict.TryGetValue(nomProd, out var producto))
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Venta: producto '{nomProd}' no encontrado."));
                return;
            }

            if (!ValidacionCargaMasiva.TryParseEntero(cantStr, out int cantidad) || cantidad <= 0)
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Venta: cantidad inválida '{cantStr}' para producto '{nomProd}'."));
                return;
            }

            if (!decimal.TryParse(precStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precioUnitario) || precioUnitario <= 0)
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Venta: precio unitario inválido '{precStr}' para producto '{nomProd}'."));
                return;
            }

            if (producto.Stock < cantidad)
            {
                resultado.Errores.Add(new ErrorFila(row,
                    $"Venta: stock insuficiente para '{nomProd}' (disponible: {producto.Stock}, solicitado: {cantidad})."));
                return;
            }

            var fechaUtc = DateTime.SpecifyKind(fechaVenta.Date, DateTimeKind.Utc);
            var ventaKey = (docCliente, fechaUtc);

            if (!ventasDict.TryGetValue(ventaKey, out var venta))
            {
                venta = new Sale
                {
                    Fecha = fechaUtc,
                    Estado = SaleStatus.Pendiente,
                    Total = 0
                };
                // Set client via navigation property so EF Core resolves FK for new clients
                if (cliente.Id > 0) venta.ClienteId = cliente.Id;
                else venta.Cliente = cliente;

                _context.Ventas.Add(venta);
                ventasDict[ventaKey] = venta;
                resultado.VentasCreadas++;
            }

            var detalle = new SaleDetail
            {
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario
            };
            if (producto.Id > 0) detalle.ProductoId = producto.Id;
            else detalle.Producto = producto;

            venta.Detalles.Add(detalle);
            venta.Total += detalle.Subtotal;
            producto.Stock -= cantidad;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Dictionary<string, int> LeerHeaders(ExcelWorksheet ws)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (ws.Dimension == null) return dict;
            for (int col = 1; col <= ws.Dimension.Columns; col++)
            {
                var val = ws.Cells[1, col].GetValue<string>();
                if (!string.IsNullOrWhiteSpace(val))
                    dict[val.Trim()] = col;
            }
            return dict;
        }

        private static string GetCellStr(ExcelWorksheet ws, int row, Dictionary<string, int> headers, string colName)
        {
            if (!headers.TryGetValue(colName, out int col)) return "";
            var value = ws.Cells[row, col].Value;
            if (value == null) return "";
            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }

        private static bool EsFilaVacia(ExcelWorksheet ws, int row, int totalCols)
        {
            for (int col = 1; col <= totalCols; col++)
                if (ws.Cells[row, col].Value != null) return false;
            return true;
        }

        private static bool TryParseDate(string s, out DateTime date)
        {
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)) return true;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaDate))
            {
                try { date = DateTime.FromOADate(oaDate); return true; }
                catch { }
            }
            date = default;
            return false;
        }

        private static string? Nulo(string s) =>
            string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
