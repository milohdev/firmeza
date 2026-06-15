using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public List<string> Categorias { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _context.Productos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                query = query.Where(p => p.Nombre.Contains(Busqueda));
            }

            if (!string.IsNullOrWhiteSpace(CategoriaFiltro))
            {
                query = query.Where(p => p.Categoria == CategoriaFiltro);
            }

            Product = await query.ToListAsync();

            Categorias = await _context.Productos
                .Where(p => p.Categoria != null)
                .Select(p => p.Categoria!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}