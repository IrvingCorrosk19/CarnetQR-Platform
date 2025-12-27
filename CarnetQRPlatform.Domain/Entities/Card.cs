namespace CarnetQRPlatform.Domain.Entities;

public class Card : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public Guid EntityProfileId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string QrToken { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Institution Institution { get; set; } = null!;
    public EntityProfile EntityProfile { get; set; } = null!;
}


