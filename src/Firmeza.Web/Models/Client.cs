using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class Client
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Documento { get; set; } = string.Empty;

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [Range(0, 120)]
    public int Edad { get; set; }

    public ICollection<Sale> Ventas { get; set; } = new List<Sale>();
}