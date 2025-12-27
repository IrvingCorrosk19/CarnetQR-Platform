using System.ComponentModel.DataAnnotations;

namespace CarnetQRPlatform.Web.Models;

public class ProfileViewModel
{
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Empresa")]
    public string? InstitutionName { get; set; }

    [Display(Name = "Último Acceso")]
    public DateTime? LastLoginAt { get; set; }
}

