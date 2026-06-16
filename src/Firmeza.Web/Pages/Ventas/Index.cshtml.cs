using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Firmeza.Web.Data;
using Firmeza.Web.Models;

namespace Firmeza.Web.Pages.Ventas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Sale> Ventas { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EstadoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Export { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busqueda))
                query = query.Where(v => v.Cliente!.Nombre.Contains(Busqueda));

            if (!string.IsNullOrWhiteSpace(EstadoFiltro) &&
                Enum.TryParse<SaleStatus>(EstadoFiltro, out var estado))
                query = query.Where(v => v.Estado == estado);

            Ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            if (Export == "excel") return ExportarExcel();

            return Page();
        }

        private IActionResult ExportarExcel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Firmeza");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Ventas");

            string[] cols = { "Fecha", "Cliente", "Total", "Estado" };
            for (int i = 0; i < cols.Length; i++)
            {
                ws.Cells[1, i + 1].Value = cols[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(255, 44, 82, 130);
                ws.Cells[1, i + 1].Style.Font.Color.SetColor(255, 255, 255, 255);
            }

            int row = 2;
            foreach (var v in Ventas)
            {
                ws.Cells[row, 1].Value = v.Fecha.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = v.Cliente?.Nombre;
                ws.Cells[row, 3].Value = (double)v.Total;
                ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 4].Value = v.Estado.ToString();
                row++;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ventas.xlsx");
        }
    }
}
