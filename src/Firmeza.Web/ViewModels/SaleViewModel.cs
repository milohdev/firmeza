using System.ComponentModel.DataAnnotations;
using Firmeza.Web.Models;

namespace Firmeza.Web.ViewModels;

public class SaleDetailViewModel
{
    [Display(Name = "Producto")]
    public int ProductoId { get; set; }

    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; }

    [Display(Name = "Precio unitario")]
    public decimal PrecioUnitario { get; set; }
}

public class SaleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria")]
    [Display(Name = "Fecha")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El cliente es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    [Display(Name = "Estado")]
    public SaleStatus Estado { get; set; } = SaleStatus.Pendiente;

    public List<SaleDetailViewModel> Detalles { get; set; } = new();
}

public class ProductoInfo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
}