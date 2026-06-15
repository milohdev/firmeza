using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;

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

    public async Task OnGetAsync()
    {
        TotalProductos = await _context.Productos.CountAsync();
        TotalClientes = await _context.Clientes.CountAsync();
        TotalVentas = await _context.Ventas.CountAsync();
    }
}