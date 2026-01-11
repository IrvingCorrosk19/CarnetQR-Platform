using System.Text.Json;

namespace CarnetQRPlatform.Domain.Entities;

public class EntityProfile : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public string? IdentificationNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhotoPath { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    public bool IsActive { get; set; } = true;
    
    // Configuración de visibilidad de datos de la entidad (por entidad, sobrescribe la global)
    public Dictionary<string, bool>? PatientDataVisibilityOverride { get; set; } // Configuración específica por entidad

    public Institution Institution { get; set; } = null!;
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<EventRecord> EventRecords { get; set; } = new List<EventRecord>();
}


