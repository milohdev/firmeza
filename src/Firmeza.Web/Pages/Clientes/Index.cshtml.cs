using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public async Task OnGetAsync()
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busqueda))
            {
                query = query.Where(c =>
                    c.Nombre.Contains(Busqueda) ||
                    c.Documento.Contains(Busqueda));
            }

            Client = await query.ToListAsync();
        }
    }
}