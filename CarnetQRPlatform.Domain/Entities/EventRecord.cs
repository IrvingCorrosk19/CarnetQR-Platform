namespace CarnetQRPlatform.Domain.Entities;

public class EventRecord : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public Guid EntityProfileId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Notes { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public Institution Institution { get; set; } = null!;
    public EntityProfile EntityProfile { get; set; } = null!;
}

public enum EventStatus
{
    Scheduled = 0,
    Completed = 1,
    NotCompleted = 2
}


