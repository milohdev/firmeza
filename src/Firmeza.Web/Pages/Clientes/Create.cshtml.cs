using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.ViewModels;

namespace Firmeza.Web.Pages.Clientes
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public ClientViewModel Cliente { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Validación de Edad con try-catch e int.Parse
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

            var client = new Client
            {
                Nombre = Cliente.Nombre,
                Documento = Cliente.Documento,
                Email = Cliente.Email,
                Telefono = Cliente.Telefono,
                Direccion = Cliente.Direccion,
                Edad = edad
            };

            _context.Clientes.Add(client);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}