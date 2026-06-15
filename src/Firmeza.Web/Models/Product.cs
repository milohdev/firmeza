using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }

    [Required]
    public int Stock { get; set; }

    [MaxLength(50)]
    public string? Categoria { get; set; }

    [MaxLength(20)]
    public string UnidadMedida { get; set; } = "Unidad";

    public ICollection<SaleDetail> DetalleVentas { get; set; } = new List<SaleDetail>();
}