namespace CarnetQRPlatform.Application.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(
        Guid institutionId,
        string? userId,
        string action,
        string entity,
        string? entityId = null,
        Dictionary<string, object>? metadata = null);
}

