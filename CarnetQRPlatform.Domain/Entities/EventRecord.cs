namespace CarnetQRPlatform.Domain.Entities;

public class EventRecord : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public Guid EntityProfileId { get; set; }
    public Guid? DoctorId { get; set; } // Médico asignado al evento (opcional)
    public DateTime ScheduledAt { get; set; }
    public string? Notes { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public Institution Institution { get; set; } = null!;
    public EntityProfile EntityProfile { get; set; } = null!;
    public Doctor? Doctor { get; set; } // Navegación al médico asignado
}

public enum EventStatus
{
    Scheduled = 0,
    Completed = 1,
    NotCompleted = 2
}


