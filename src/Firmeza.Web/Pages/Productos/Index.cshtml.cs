using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Firmeza.Web.Data;
using Firmeza.Web.Models;

namespace Firmeza.Web.Pages.Productos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Product> Product { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoriaFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Export { get; set; }

        public List<string> Categorias { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Productos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busqueda))
                query = query.Where(p => p.Nombre.Contains(Busqueda));

            if (!string.IsNullOrWhiteSpace(CategoriaFiltro))
                query = query.Where(p => p.Categoria == CategoriaFiltro);

            Product = await query.ToListAsync();

            if (Export == "excel") return ExportarExcel();
            if (Export == "pdf") return ExportarPdf();

            Categorias = await _context.Productos
                .Where(p => p.Categoria != null)
                .Select(p => p.Categoria!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Page();
        }

        private IActionResult ExportarExcel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Firmeza");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Productos");

            string[] cols = { "Nombre", "Descripción", "Precio", "Stock", "Categoría", "Unidad de medida" };
            for (int i = 0; i < cols.Length; i++)
            {
                ws.Cells[1, i + 1].Value = cols[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(255, 44, 82, 130);
                ws.Cells[1, i + 1].Style.Font.Color.SetColor(255, 255, 255, 255);
            }

            int row = 2;
            foreach (var p in Product)
            {
                ws.Cells[row, 1].Value = p.Nombre;
                ws.Cells[row, 2].Value = p.Descripcion;
                ws.Cells[row, 3].Value = (double)p.Precio;
                ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 4].Value = p.Stock;
                ws.Cells[row, 5].Value = p.Categoria;
                ws.Cells[row, 6].Value = p.UnidadMedida;
                row++;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "productos.xlsx");
        }

        private IActionResult ExportarPdf()
        {
            var productos = Product;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica"));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Firmeza — Listado de Productos")
                            .FontSize(16).Bold().FontColor("#2C5282");
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor("#718096");

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                foreach (var titulo in new[] { "Nombre", "Descripción", "Precio", "Stock", "Categoría", "Unidad" })
                                    h.Cell().Background("#2C5282").Padding(5)
                                        .Text(x => x.Span(titulo).Bold().FontSize(9).FontColor(Colors.White));
                            });

                            bool par = false;
                            foreach (var p in productos)
                            {
                                string bg = par ? "#EBF8FF" : Colors.White;
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(p.Nombre).FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(p.Descripcion ?? "").FontSize(8));
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(x => x.Span(p.Precio.ToString("C")).FontSize(8));
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(x => x.Span(p.Stock.ToString()).FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(p.Categoria ?? "").FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(p.UnidadMedida).FontSize(8));
                                par = !par;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Firmeza · Página ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" de ").FontSize(8);
                        x.TotalPages().FontSize(8);
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", "productos.pdf");
        }
    }
}
