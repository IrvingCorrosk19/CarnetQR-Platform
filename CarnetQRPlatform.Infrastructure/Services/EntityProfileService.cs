using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class EntityProfileService : IEntityProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public EntityProfileService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<EntityProfile>> GetAllAsync()
    {
        var query = _context.EntityProfiles.Include(e => e.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync();
    }

    public async Task<EntityProfile?> GetByIdAsync(Guid id)
    {
        var query = _context.EntityProfiles.Include(e => e.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EntityProfile> CreateAsync(EntityProfile entityProfile)
    {
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        // Si no hay tenant y no es SuperAdmin, rechazar
        if (!tenantId.HasValue && !_tenantProvider.IsSuperAdmin())
        {
            throw new InvalidOperationException("Cannot create entity profile without tenant context");
        }
        
        // Si es SuperAdmin, debe proporcionar InstitutionId explícitamente
        if (_tenantProvider.IsSuperAdmin() && entityProfile.InstitutionId == Guid.Empty)
        {
            throw new InvalidOperationException("SuperAdmin must specify InstitutionId when creating entity profiles");
        }
        
        // FORZAR InstitutionId desde el tenant (ignorar cualquier valor que venga del request)
        if (tenantId.HasValue)
        {
            entityProfile.InstitutionId = tenantId.Value;
        }

        entityProfile.Id = Guid.NewGuid();
        entityProfile.CreatedAt = DateTime.UtcNow;
        
        // Convertir DateOfBirth a UTC si está presente (PostgreSQL requiere UTC para timestamp with time zone)
        if (entityProfile.DateOfBirth.HasValue && entityProfile.DateOfBirth.Value.Kind != DateTimeKind.Utc)
        {
            entityProfile.DateOfBirth = DateTime.SpecifyKind(entityProfile.DateOfBirth.Value, DateTimeKind.Utc);
        }
        
        _context.EntityProfiles.Add(entityProfile);
        await _context.SaveChangesAsync();
        
        return entityProfile;
    }

    public async Task<EntityProfile> UpdateAsync(EntityProfile entityProfile)
    {
        // MULTI-TENANT ESTRICTO: Obtener entidad existente con filtro de tenant
        var existing = await GetByIdAsync(entityProfile.Id);
        if (existing == null)
        {
            throw new ArgumentException("Entity profile not found or access denied");
        }

        // VALIDAR que el InstitutionId no haya cambiado (protección adicional)
        if (existing.InstitutionId != entityProfile.InstitutionId)
        {
            throw new InvalidOperationException(
                $"Multi-tenant violation: Cannot change InstitutionId from {existing.InstitutionId} to {entityProfile.InstitutionId}");
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva del existing)
        existing.IdentificationNumber = entityProfile.IdentificationNumber;
        existing.FirstName = entityProfile.FirstName;
        existing.LastName = entityProfile.LastName;
        existing.Email = entityProfile.Email;
        existing.Phone = entityProfile.Phone;
        existing.DateOfBirth = entityProfile.DateOfBirth;
        existing.PhotoPath = entityProfile.PhotoPath;
        existing.CustomFields = entityProfile.CustomFields;
        existing.IsActive = entityProfile.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        // Convertir DateOfBirth a UTC si está presente
        if (existing.DateOfBirth.HasValue && existing.DateOfBirth.Value.Kind != DateTimeKind.Utc)
        {
            existing.DateOfBirth = DateTime.SpecifyKind(existing.DateOfBirth.Value, DateTimeKind.Utc);
        }

        _context.EntityProfiles.Update(existing);
        await _context.SaveChangesAsync();
        
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        System.Console.WriteLine("=== [EntityProfileService] DeleteAsync ===");
        System.Console.WriteLine($"[Service] DeleteAsync called with ID: {id}");
        
        System.Console.WriteLine("[Service] Getting entity by ID...");
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            System.Console.WriteLine("[Service] Entity not found!");
            return false;
        }

        System.Console.WriteLine($"[Service] Entity found: {entity.FirstName} {entity.LastName}, InstitutionId: {entity.InstitutionId}");

        // Validar si tiene carnets asociados
        System.Console.WriteLine("[Service] Checking for associated cards...");
        var hasCards = await _context.Cards
            .AnyAsync(c => c.EntityProfileId == id);
        
        System.Console.WriteLine($"[Service] Has cards: {hasCards}");
        
        if (hasCards)
        {
            System.Console.WriteLine("[Service] Cannot delete: Has cards associated");
            throw new InvalidOperationException(
                "No se puede eliminar la entidad porque tiene carnets asociados. Elimine primero los carnets.");
        }

        // Validar si tiene eventos asociados
        System.Console.WriteLine("[Service] Checking for associated events...");
        var hasEvents = await _context.EventRecords
            .AnyAsync(e => e.EntityProfileId == id);
        
        System.Console.WriteLine($"[Service] Has events: {hasEvents}");
        
        if (hasEvents)
        {
            System.Console.WriteLine("[Service] Cannot delete: Has events associated");
            throw new InvalidOperationException(
                "No se puede eliminar la entidad porque tiene eventos asociados. Elimine primero los eventos.");
        }

        System.Console.WriteLine("[Service] No restrictions found, proceeding with deletion...");
        _context.EntityProfiles.Remove(entity);
        
        System.Console.WriteLine("[Service] Saving changes...");
        await _context.SaveChangesAsync();
        
        System.Console.WriteLine("[Service] Delete successful!");
        System.Console.WriteLine("=== [EntityProfileService] DeleteAsync END ===");
        
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        _context.EntityProfiles.Update(entity);
        await _context.SaveChangesAsync();
        
        return true;
    }
}

