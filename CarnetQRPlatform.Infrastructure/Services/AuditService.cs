using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(
        Guid institutionId,
        string? userId,
        string action,
        string entity,
        string? entityId = null,
        Dictionary<string, object>? metadata = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}

