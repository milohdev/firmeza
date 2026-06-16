using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;
using Firmeza.Web.Models;

namespace Firmeza.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalProductos { get; set; }
    public int TotalClientes { get; set; }
    public int TotalVentas { get; set; }
    public List<Sale> UltimasVentas { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalProductos = await _context.Productos.CountAsync();
        TotalClientes = await _context.Clientes.CountAsync();
        TotalVentas = await _context.Ventas.CountAsync();

        UltimasVentas = await _context.Ventas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.Fecha)
            .Take(5)
            .ToListAsync();
    }
}
