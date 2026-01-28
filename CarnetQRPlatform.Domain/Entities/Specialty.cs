namespace CarnetQRPlatform.Domain.Entities;

public class Specialty : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Relaciones
    public Institution Institution { get; set; } = null!;
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
