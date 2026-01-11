namespace CarnetQRPlatform.Domain.Entities;

public class InstitutionType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Relación con instituciones
    public ICollection<Institution> Institutions { get; set; } = new List<Institution>();
}

