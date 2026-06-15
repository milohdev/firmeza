using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class SaleDetail
{
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }
    public Sale? Venta { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public Product? Producto { get; set; }

    [Required]
    public int Cantidad { get; set; }

    [Required]
    public decimal PrecioUnitario { get; set; }
    
    public decimal Subtotal => Cantidad * PrecioUnitario;

}