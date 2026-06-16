using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.ViewModels;

namespace Firmeza.Web.Pages.Ventas
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SaleViewModel Venta { get; set; } = default!;

        public List<SelectListItem> ClientesOptions { get; set; } = new();
        public List<ProductoInfo> ProductosLista { get; set; } = new();
        public string ProductosJson { get; set; } = "[]";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var sale = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (sale == null) return NotFound();

            Venta = new SaleViewModel
            {
                Id = sale.Id,
                Fecha = sale.Fecha,
                ClienteId = sale.ClienteId,
                Estado = sale.Estado,
                Detalles = sale.Detalles.Select(d => new SaleDetailViewModel
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList()
            };

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

            var sale = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == Venta.Id);

            if (sale == null) return NotFound();

            var allProductIds = sale.Detalles.Select(d => d.ProductoId)
                .Union(Venta.Detalles.Select(d => d.ProductoId))
                .Distinct()
                .ToList();

            var productos = await _context.Productos
                .Where(p => allProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var detalle in sale.Detalles)
            {
                if (productos.TryGetValue(detalle.ProductoId, out var p))
                    p.Stock += detalle.Cantidad;
            }

            _context.DetalleVentas.RemoveRange(sale.Detalles);
            sale.Detalles.Clear();

            foreach (var d in Venta.Detalles)
            {
                if (!productos.TryGetValue(d.ProductoId, out var producto) || producto.Stock < d.Cantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para '{productos.GetValueOrDefault(d.ProductoId)?.Nombre ?? "producto desconocido"}'.");
                    await CargarDropdowns();
                    return Page();
                }
            }

            foreach (var d in Venta.Detalles)
            {
                sale.Detalles.Add(new SaleDetail
                {
                    VentaId = sale.Id,
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                });
                productos[d.ProductoId].Stock -= d.Cantidad;
            }

            sale.Fecha = Venta.Fecha;
            sale.ClienteId = Venta.ClienteId;
            sale.Estado = Venta.Estado;
            sale.Total = sale.Detalles.Sum(d => d.Subtotal);

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
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