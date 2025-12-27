using System.Text.Json;

namespace CarnetQRPlatform.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}


