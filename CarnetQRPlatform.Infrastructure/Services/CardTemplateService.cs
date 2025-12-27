using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class CardTemplateService : ICardTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CardTemplateService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<CardTemplate>> GetAllAsync()
    {
        var query = _context.CardTemplates
            .Include(t => t.Institution)
            .AsQueryable();
        
        return await query.ApplyTenantFilter(_tenantProvider)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<CardTemplate?> GetByIdAsync(Guid id)
    {
        var query = _context.CardTemplates
            .Include(t => t.Institution)
            .AsQueryable();
        
        return await query.ApplyTenantFilter(_tenantProvider)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<CardTemplate?> GetDefaultTemplateAsync()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return null;
        }

        var template = await _context.CardTemplates
            .Include(t => t.Institution)
            .FirstOrDefaultAsync(t => t.InstitutionId == tenantId.Value && t.IsDefault);

        if (template == null)
        {
            // Si no hay plantilla por defecto, devolver la primera disponible
            template = await _context.CardTemplates
                .Include(t => t.Institution)
                .FirstOrDefaultAsync(t => t.InstitutionId == tenantId.Value);
        }

        return template;
    }

    public async Task<CardTemplate> CreateAsync(CardTemplate template)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Cannot create template without tenant context");
        }

        // Validar máximo 6 campos visibles
        if (template.VisibleFields.Count > 6)
        {
            throw new ArgumentException("Maximum 6 visible fields allowed");
        }

        template.Id = Guid.NewGuid();
        template.InstitutionId = tenantId.Value;
        template.CreatedAt = DateTime.UtcNow;

        // Si es el primero de la institución o se marca como default, marcar como default
        var existingTemplates = await _context.CardTemplates
            .Where(t => t.InstitutionId == tenantId.Value)
            .CountAsync();

        if (existingTemplates == 0 || template.IsDefault)
        {
            // Desmarcar otros templates como default si este es default
            if (template.IsDefault)
            {
                var otherTemplates = await _context.CardTemplates
                    .Where(t => t.InstitutionId == tenantId.Value && t.IsDefault)
                    .ToListAsync();
                
                foreach (var other in otherTemplates)
                {
                    other.IsDefault = false;
                }
            }
            template.IsDefault = true;
        }

        _context.CardTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template;
    }

    public async Task<CardTemplate> UpdateAsync(CardTemplate template)
    {
        // MULTI-TENANT ESTRICTO: GetByIdAsync ya aplica filtro de tenant
        var existing = await GetByIdAsync(template.Id);
        if (existing == null)
        {
            throw new ArgumentException("Template not found or access denied");
        }

        // VALIDAR que el InstitutionId no haya cambiado (protección adicional)
        if (existing.InstitutionId != template.InstitutionId)
        {
            throw new InvalidOperationException(
                $"Multi-tenant violation: Cannot change InstitutionId from {existing.InstitutionId} to {template.InstitutionId}");
        }

        // Validar máximo 6 campos visibles
        if (template.VisibleFields.Count > 6)
        {
            throw new ArgumentException("Maximum 6 visible fields allowed");
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva del existing)
        existing.Name = template.Name;
        existing.PhotoEnabled = template.PhotoEnabled;
        existing.VisibleFields = template.VisibleFields;
        existing.TemplateHtml = template.TemplateHtml;
        existing.TemplateConfig = template.TemplateConfig;
        existing.UpdatedAt = DateTime.UtcNow;

        // Si se marca como default, desmarcar otros
        if (template.IsDefault && !existing.IsDefault)
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                var otherTemplates = await _context.CardTemplates
                    .Where(t => t.InstitutionId == tenantId.Value && t.Id != template.Id && t.IsDefault)
                    .ToListAsync();
                
                foreach (var other in otherTemplates)
                {
                    other.IsDefault = false;
                }
            }
            existing.IsDefault = true;
        }
        else if (!template.IsDefault && existing.IsDefault)
        {
            // No permitir desmarcar si es el único template
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                var templateCount = await _context.CardTemplates
                    .Where(t => t.InstitutionId == tenantId.Value)
                    .CountAsync();
                
                if (templateCount > 1)
                {
                    existing.IsDefault = false;
                    // Marcar el primero disponible como default
                    var firstTemplate = await _context.CardTemplates
                        .Where(t => t.InstitutionId == tenantId.Value && t.Id != template.Id)
                        .FirstOrDefaultAsync();
                    if (firstTemplate != null)
                    {
                        firstTemplate.IsDefault = true;
                    }
                }
            }
        }

        _context.CardTemplates.Update(existing);
        await _context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template == null) return false;

        // No permitir eliminar si es el único template
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (tenantId.HasValue)
        {
            var templateCount = await _context.CardTemplates
                .Where(t => t.InstitutionId == tenantId.Value)
                .CountAsync();
            
            if (templateCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last template");
            }
        }

        _context.CardTemplates.Remove(template);
        await _context.SaveChangesAsync();

        // Si era el default, marcar otro como default
        if (template.IsDefault && tenantId.HasValue)
        {
            var firstTemplate = await _context.CardTemplates
                .Where(t => t.InstitutionId == tenantId.Value)
                .FirstOrDefaultAsync();
            if (firstTemplate != null)
            {
                firstTemplate.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> SetAsDefaultAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template == null) return false;

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        // Desmarcar otros templates
        var otherTemplates = await _context.CardTemplates
            .Where(t => t.InstitutionId == tenantId.Value && t.Id != id && t.IsDefault)
            .ToListAsync();
        
        foreach (var other in otherTemplates)
        {
            other.IsDefault = false;
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}


