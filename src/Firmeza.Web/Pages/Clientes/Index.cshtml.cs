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

namespace Firmeza.Web.Pages.Clientes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Client> Client { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Export { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busqueda))
                query = query.Where(c =>
                    c.Nombre.Contains(Busqueda) ||
                    c.Documento.Contains(Busqueda));

            Client = await query.ToListAsync();

            if (Export == "excel") return ExportarExcel();
            if (Export == "pdf") return ExportarPdf();

            return Page();
        }

        private IActionResult ExportarExcel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Firmeza");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Clientes");

            string[] cols = { "Nombre", "Documento", "Email", "Teléfono", "Dirección", "Edad" };
            for (int i = 0; i < cols.Length; i++)
            {
                ws.Cells[1, i + 1].Value = cols[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(255, 44, 82, 130);
                ws.Cells[1, i + 1].Style.Font.Color.SetColor(255, 255, 255, 255);
            }

            int row = 2;
            foreach (var c in Client)
            {
                ws.Cells[row, 1].Value = c.Nombre;
                ws.Cells[row, 2].Value = c.Documento;
                ws.Cells[row, 3].Value = c.Email;
                ws.Cells[row, 4].Value = c.Telefono;
                ws.Cells[row, 5].Value = c.Direccion;
                ws.Cells[row, 6].Value = c.Edad;
                row++;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "clientes.xlsx");
        }

        private IActionResult ExportarPdf()
        {
            var clientes = Client;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica"));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Firmeza — Listado de Clientes")
                            .FontSize(16).Bold().FontColor("#2C5282");
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor("#718096");

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(4);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                foreach (var titulo in new[] { "Nombre", "Documento", "Email", "Teléfono", "Dirección", "Edad" })
                                    h.Cell().Background("#2C5282").Padding(5)
                                        .Text(x => x.Span(titulo).Bold().FontSize(9).FontColor(Colors.White));
                            });

                            bool par = false;
                            foreach (var c in clientes)
                            {
                                string bg = par ? "#EBF8FF" : Colors.White;
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(c.Nombre).FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(c.Documento).FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(c.Email ?? "").FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(c.Telefono ?? "").FontSize(8));
                                table.Cell().Background(bg).Padding(4).Text(x => x.Span(c.Direccion ?? "").FontSize(8));
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(x => x.Span(c.Edad.ToString()).FontSize(8));
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

            return File(pdf, "application/pdf", "clientes.pdf");
        }
    }
}
