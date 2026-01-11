using System.ComponentModel.DataAnnotations;

namespace CarnetQRPlatform.Web.Models;

public class CreateInstitutionViewModel
{
    [Required]
    [Display(Name = "Nombre de la Empresa")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [EmailAddress]
    [Display(Name = "Correo Electrónico")]
    public string? Email { get; set; }

    [Display(Name = "Teléfono")]
    public string? Phone { get; set; }

    [Display(Name = "Dirección")]
    public string? Address { get; set; }

    [Required]
    [MaxLength(10)]
    [Display(Name = "Prefijo de Carnet")]
    public string CardPrefix { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tipo de Institución")]
    public Guid? InstitutionTypeId { get; set; }

    // Datos del Administrador
    [Required]
    [EmailAddress]
    [Display(Name = "Email del Administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña del Administrador")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nombre del Administrador")]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Apellido del Administrador")]
    public string AdminLastName { get; set; } = string.Empty;
}

