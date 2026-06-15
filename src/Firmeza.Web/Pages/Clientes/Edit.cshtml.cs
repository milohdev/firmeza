using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.ViewModels;

namespace Firmeza.Web.Pages.Clientes
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ClientViewModel Cliente { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clientes.FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            Cliente = new ClientViewModel
            {
                Id = client.Id,
                Nombre = client.Nombre,
                Documento = client.Documento,
                Email = client.Email,
                Telefono = client.Telefono,
                Direccion = client.Direccion,
                EdadInput = client.Edad.ToString()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int edad;
            try
            {
                edad = int.Parse(Cliente.EdadInput);

                if (edad < 0 || edad > 120)
                {
                    ModelState.AddModelError("Cliente.EdadInput", "La edad debe estar entre 0 y 120 años.");
                }
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Cliente.EdadInput", "La edad debe ser un número entero válido.");
                edad = 0;
            }
            catch (OverflowException)
            {
                ModelState.AddModelError("Cliente.EdadInput", "El número ingresado es demasiado grande.");
                edad = 0;
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = await _context.Clientes.FindAsync(Cliente.Id);
            if (client == null)
            {
                return NotFound();
            }

            client.Nombre = Cliente.Nombre;
            client.Documento = Cliente.Documento;
            client.Email = Cliente.Email;
            client.Telefono = Cliente.Telefono;
            client.Direccion = Cliente.Direccion;
            client.Edad = edad;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(Cliente.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClientExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }
    }
}