using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;


public enum SaleStatus
{
    Pendiente,
    Pagada,
    Anulada
}

public class Sale
{
    public int Id { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    public int ClienteId { get; set; }
    public Client? Cliente { get; set; }

    [Required]
    public decimal Total { get; set; }

    [Required]
    public SaleStatus Estado { get; set; } = SaleStatus.Pendiente;

    public ICollection<SaleDetail> Detalles { get; set; } = new List<SaleDetail>();
}