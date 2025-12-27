namespace CarnetQRPlatform.Domain.Entities;

public class Institution : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string CardPrefix { get; set; } = string.Empty;
    public string? InstitutionType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LogoPath { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<EntityProfile> EntityProfiles { get; set; } = new List<EntityProfile>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<CardTemplate> CardTemplates { get; set; } = new List<CardTemplate>();
    public ICollection<EventRecord> EventRecords { get; set; } = new List<EventRecord>();
}


