using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public EventService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<EventRecord>> GetAllAsync()
    {
        var query = _context.EventRecords.Include(e => e.EntityProfile).Include(e => e.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).OrderByDescending(e => e.ScheduledAt).ToListAsync();
    }

    public async Task<IEnumerable<EventRecord>> GetUpcomingAsync()
    {
        var query = _context.EventRecords
            .Include(e => e.EntityProfile)
            .Where(e => e.ScheduledAt >= DateTime.UtcNow && e.Status == EventStatus.Scheduled)
            .AsQueryable();

        return await query.ApplyTenantFilter(_tenantProvider).OrderBy(e => e.ScheduledAt).ToListAsync();
    }

    public async Task<IEnumerable<EventRecord>> GetByEntityProfileAsync(Guid entityProfileId)
    {
        var query = _context.EventRecords
            .Include(e => e.EntityProfile)
            .Where(e => e.EntityProfileId == entityProfileId)
            .AsQueryable();

        return await query.ApplyTenantFilter(_tenantProvider).OrderByDescending(e => e.ScheduledAt).ToListAsync();
    }

    public async Task<EventRecord?> GetByIdAsync(Guid id)
    {
        var query = _context.EventRecords.Include(e => e.EntityProfile).Include(e => e.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EventRecord> CreateAsync(EventRecord eventRecord)
    {
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        // Si no hay tenant y no es SuperAdmin, rechazar
        if (!tenantId.HasValue && !_tenantProvider.IsSuperAdmin())
        {
            throw new InvalidOperationException("Cannot create event record without tenant context");
        }
        
        // Si es SuperAdmin, debe proporcionar InstitutionId explícitamente
        if (_tenantProvider.IsSuperAdmin() && eventRecord.InstitutionId == Guid.Empty)
        {
            throw new InvalidOperationException("SuperAdmin must specify InstitutionId when creating event records");
        }
        
        // FORZAR InstitutionId desde el tenant (ignorar cualquier valor que venga del request)
        if (tenantId.HasValue)
        {
            eventRecord.InstitutionId = tenantId.Value;
        }

        // VALIDAR que EntityProfile pertenece al mismo tenant
        if (tenantId.HasValue)
        {
            var entityProfile = await _context.EntityProfiles
                .FirstOrDefaultAsync(ep => ep.Id == eventRecord.EntityProfileId && ep.InstitutionId == tenantId.Value);
            
            if (entityProfile == null)
            {
                throw new InvalidOperationException(
                    "Entity profile not found or does not belong to the current tenant");
            }
        }

        eventRecord.Id = Guid.NewGuid();
        eventRecord.Status = EventStatus.Scheduled;
        eventRecord.CreatedAt = DateTime.UtcNow;
        _context.EventRecords.Add(eventRecord);
        await _context.SaveChangesAsync();
        return eventRecord;
    }

    public async Task<EventRecord> UpdateAsync(EventRecord eventRecord)
    {
        // MULTI-TENANT ESTRICTO: GetByIdAsync ya aplica filtro de tenant
        var existingEvent = await GetByIdAsync(eventRecord.Id);
        if (existingEvent == null)
        {
            throw new ArgumentException("EventRecord not found or access denied");
        }

        // No permitir editar eventos completados
        if (existingEvent.Status != EventStatus.Scheduled)
        {
            throw new InvalidOperationException("Solo se pueden editar eventos programados.");
        }

        // VALIDAR que EntityProfile pertenece al mismo tenant
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (tenantId.HasValue)
        {
            var entityProfile = await _context.EntityProfiles
                .FirstOrDefaultAsync(ep => ep.Id == eventRecord.EntityProfileId && ep.InstitutionId == tenantId.Value);
            
            if (entityProfile == null)
            {
                throw new InvalidOperationException(
                    "Entity profile not found or does not belong to the current tenant");
            }
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva, no se puede cambiar)
        existingEvent.EntityProfileId = eventRecord.EntityProfileId;
        existingEvent.ScheduledAt = eventRecord.ScheduledAt;
        existingEvent.Notes = eventRecord.Notes;
        existingEvent.UpdatedAt = DateTime.UtcNow;
        
        _context.EventRecords.Update(existingEvent);
        await _context.SaveChangesAsync();
        return existingEvent;
    }

    public async Task<EventRecord> UpdateStatusAsync(Guid id, EventStatus status)
    {
        // MULTI-TENANT ESTRICTO: GetByIdAsync ya aplica filtro de tenant
        var eventRecord = await GetByIdAsync(id);
        if (eventRecord == null)
        {
            throw new ArgumentException("EventRecord not found or access denied");
        }

        if (status != EventStatus.Scheduled && eventRecord.ScheduledAt > DateTime.UtcNow)
        {
            throw new InvalidOperationException("No se puede cambiar el estado de un evento antes de su fecha programada");
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva)
        eventRecord.Status = status;
        if (status != EventStatus.Scheduled)
        {
            eventRecord.CompletedAt = DateTime.UtcNow;
        }
        eventRecord.UpdatedAt = DateTime.UtcNow;
        
        _context.EventRecords.Update(eventRecord);
        await _context.SaveChangesAsync();
        return eventRecord;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var eventRecord = await GetByIdAsync(id);
        if (eventRecord == null) return false;

        _context.EventRecords.Remove(eventRecord);
        await _context.SaveChangesAsync();
        return true;
    }
}

