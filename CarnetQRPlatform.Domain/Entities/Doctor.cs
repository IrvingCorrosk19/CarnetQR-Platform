namespace CarnetQRPlatform.Domain.Entities;

public class Doctor : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public Guid SpecialtyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; } // Número de licencia médica
    public bool IsActive { get; set; } = true;

    // Relaciones
    public Institution Institution { get; set; } = null!;
    public Specialty Specialty { get; set; } = null!;
}
