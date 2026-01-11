using System.Text.Json;
using CarnetQRPlatform.Domain.Enums;

namespace CarnetQRPlatform.Domain.Entities;

public class Institution : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string CardPrefix { get; set; } = string.Empty;
    public Guid? InstitutionTypeId { get; set; }
    public InstitutionType? InstitutionType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LogoPath { get; set; }
    
    // Configuración del Carnet
    public bool PhotoEnabled { get; set; } = false; // Si el carnet incluye foto de la entidad
    public List<string> VisibleFields { get; set; } = new(); // Hasta 6 campos visibles en el carnet
    
    // Configuración del QR Público
    public QrPublicDisplayMode QrPublicDisplayMode { get; set; } = QrPublicDisplayMode.CardNumber; // Nombre o número de carnet
    public string? Instructions { get; set; } // Información fija (teléfono, dirección, indicaciones) para QR público
    
    // Configuración de visibilidad de datos de la entidad (global)
    public Dictionary<string, bool> PatientDataVisibilityConfig { get; set; } = new(); // Configuración global de visibilidad

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<EntityProfile> EntityProfiles { get; set; } = new List<EntityProfile>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<CardTemplate> CardTemplates { get; set; } = new List<CardTemplate>();
    public ICollection<EventRecord> EventRecords { get; set; } = new List<EventRecord>();
}

public enum QrPublicDisplayMode
{
    CardNumber = 0, // Mostrar número de carnet
    PatientName = 1 // Mostrar nombre de la entidad
}


