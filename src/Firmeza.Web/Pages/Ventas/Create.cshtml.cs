using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.ViewModels;

namespace Firmeza.Web.Pages.Ventas
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public SaleViewModel Venta { get; set; } = default!;

        public List<SelectListItem> ClientesOptions { get; set; } = new();
        public List<ProductoInfo> ProductosLista { get; set; } = new();
        public string ProductosJson { get; set; } = "[]";

        public async Task<IActionResult> OnGetAsync()
        {
            Venta = new SaleViewModel { Fecha = DateTime.Today };
            await CargarDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Venta.Detalles = Venta.Detalles
                .Where(d => d.ProductoId > 0 && d.Cantidad > 0)
                .ToList();

            if (!ModelState.IsValid)
            {
                await CargarDropdowns();
                return Page();
            }

            if (!Venta.Detalles.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto a la venta.");
                await CargarDropdowns();
                return Page();
            }

            var productoIds = Venta.Detalles.Select(d => d.ProductoId).Distinct().ToList();
            var productos = await _context.Productos
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var d in Venta.Detalles)
            {
                if (!productos.TryGetValue(d.ProductoId, out var producto) || producto.Stock < d.Cantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para '{productos.GetValueOrDefault(d.ProductoId)?.Nombre ?? "producto desconocido"}'.");
                    await CargarDropdowns();
                    return Page();
                }
            }

            var sale = new Sale
            {
                Fecha = DateTime.SpecifyKind(Venta.Fecha, DateTimeKind.Utc),
                ClienteId = Venta.ClienteId,
                Estado = SaleStatus.Pendiente,
                Total = 0
            };

            foreach (var d in Venta.Detalles)
            {
                sale.Detalles.Add(new SaleDetail
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                });
                productos[d.ProductoId].Stock -= d.Cantidad;
            }

            sale.Total = sale.Detalles.Sum(d => d.Subtotal);
            _context.Ventas.Add(sale);
            await _context.SaveChangesAsync();

            int saleId = sale.Id;

            try
            {
                var saleCompleta = await _context.Ventas
                    .Include(v => v.Cliente)
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstAsync(v => v.Id == saleId);

                GenerarRecibo(saleCompleta);
            }
            catch
            {
                // PDF no es crítico — la venta ya fue guardada
            }

            TempData["Exito"] = $"Venta #{saleId} creada exitosamente.";
            return RedirectToPage("./Details", new { id = saleId });
        }

        private void GenerarRecibo(Sale sale)
        {
            var recibosDir = Path.Combine(_env.WebRootPath, "recibos");
            Directory.CreateDirectory(recibosDir);

            decimal subtotal = sale.Detalles.Sum(d => d.Subtotal);
            decimal iva = Math.Round(subtotal * 0.19m, 2);
            decimal total = subtotal + iva;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Encabezado
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FIRMEZA").FontSize(22).Bold().FontColor("#2C5282");
                                c.Item().Text("Recibo de Venta").FontSize(12).FontColor("#4A5568");
                            });
                            row.ConstantItem(130).Column(c =>
                            {
                                c.Item().AlignRight().Text($"Venta # {sale.Id}").Bold();
                                c.Item().AlignRight().Text($"Fecha: {sale.Fecha:dd/MM/yyyy}").FontColor("#718096");
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CBD5E0");

                        // Datos del cliente
                        col.Item().PaddingTop(8).Column(c =>
                        {
                            c.Item().Text("CLIENTE").Bold().FontSize(9).FontColor("#718096");
                            c.Item().Text(sale.Cliente?.Nombre ?? "—").Bold();
                            if (!string.IsNullOrEmpty(sale.Cliente?.Documento))
                                c.Item().Text($"Doc: {sale.Cliente.Documento}").FontColor("#4A5568");
                            if (!string.IsNullOrEmpty(sale.Cliente?.Email))
                                c.Item().Text($"Email: {sale.Cliente.Email}").FontColor("#4A5568");
                            if (!string.IsNullOrEmpty(sale.Cliente?.Telefono))
                                c.Item().Text($"Tel: {sale.Cliente.Telefono}").FontColor("#4A5568");
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                foreach (var titulo in new[] { "Producto", "Cant.", "P. Unitario", "Subtotal" })
                                    h.Cell().Background("#2C5282").Padding(5)
                                        .Text(x => x.Span(titulo).Bold().FontSize(8).FontColor(Colors.White));
                            });

                            bool par = false;
                            foreach (var d in sale.Detalles)
                            {
                                string bg = par ? "#EBF8FF" : Colors.White;
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(d.Producto?.Nombre ?? "").FontSize(9));
                                table.Cell().Background(bg).Padding(4).AlignCenter().Text(x => x.Span(d.Cantidad.ToString()).FontSize(9));
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(x => x.Span(d.PrecioUnitario.ToString("C")).FontSize(9));
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(x => x.Span(d.Subtotal.ToString("C")).FontSize(9));
                                par = !par;
                            }
                        });

                        // Subtotales con IVA
                        col.Item().PaddingTop(6).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Subtotal: {subtotal:C}").FontSize(9);
                            c.Item().AlignRight().Text($"IVA (19%): {iva:C}").FontSize(9);
                            c.Item().PaddingTop(2).LineHorizontal(1).LineColor("#CBD5E0");
                            c.Item().AlignRight().Text($"TOTAL: {total:C}").Bold().FontSize(12);
                        });
                    });

                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor("#CBD5E0");
                        c.Item().PaddingTop(4).AlignCenter()
                            .Text("Gracias por su compra").Bold().FontColor("#2C5282");
                        c.Item().AlignCenter()
                            .Text("Firmeza · Tu ferretería de confianza").FontSize(8).FontColor("#718096");
                    });
                });
            }).GeneratePdf();

            System.IO.File.WriteAllBytes(Path.Combine(recibosDir, $"recibo-{sale.Id}.pdf"), pdf);
        }

        private async Task CargarDropdowns()
        {
            ClientesOptions = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre })
                .ToListAsync();

            ProductosLista = await _context.Productos
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoInfo { Id = p.Id, Nombre = p.Nombre, Precio = p.Precio })
                .ToListAsync();

            ProductosJson = JsonSerializer.Serialize(
                ProductosLista.Select(p => new { id = p.Id, nombre = p.Nombre, precio = p.Precio })
            );
        }
    }
}
