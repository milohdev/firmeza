using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.ViewModels;

public class ClientViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio")]
    [StringLength(20)]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "El documento solo debe contener números")]
    [Display(Name = "Documento")]
    public string Documento { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [StringLength(150)]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido")]
    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(250)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    // La edad se maneja como string en el ViewModel para poder
    // capturar el error de conversión con try-catch en el PageModel
    [Required(ErrorMessage = "La edad es obligatoria")]
    [Display(Name = "Edad")]
    public string EdadInput { get; set; } = string.Empty;
}