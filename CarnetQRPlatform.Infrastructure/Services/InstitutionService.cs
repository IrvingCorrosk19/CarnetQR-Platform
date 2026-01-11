using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;

namespace CarnetQRPlatform.Infrastructure.Services;

public class InstitutionService : IInstitutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICacheService _cacheService;

    public InstitutionService(ApplicationDbContext context, ITenantProvider tenantProvider, ICacheService cacheService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<Institution>> GetAllAsync()
    {
        const string cacheKey = "institutions_all";
        
        // Intentar obtener desde caché
        var cached = await _cacheService.GetAsync<IEnumerable<Institution>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Si no está en caché, obtener de base de datos
        var institutions = await _context.Institutions
            .OrderBy(i => i.Name)
            .ToListAsync();

        // Guardar en caché por 30 minutos
        await _cacheService.SetAsync(cacheKey, institutions, TimeSpan.FromMinutes(30));
        
        return institutions;
    }

    public async Task<Institution?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"institution_{id}";
        
        // Intentar obtener desde caché
        var cached = await _cacheService.GetAsync<Institution>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Si no está en caché, obtener de base de datos
        var institution = await _context.Institutions.FindAsync(id);
        
        if (institution != null)
        {
            // Guardar en caché por 30 minutos
            await _cacheService.SetAsync(cacheKey, institution, TimeSpan.FromMinutes(30));
        }

        return institution;
    }

    public async Task<Institution> CreateAsync(Institution institution)
    {
        // Verificar si el CardPrefix ya existe
        var existingInstitution = await _context.Institutions
            .FirstOrDefaultAsync(i => i.CardPrefix == institution.CardPrefix);
        
        if (existingInstitution != null)
        {
            throw new InvalidOperationException(
                $"El prefijo de carnet '{institution.CardPrefix}' ya está en uso por la institución '{existingInstitution.Name}'. Por favor, elija otro prefijo.");
        }
        
        institution.Id = Guid.NewGuid();
        institution.CreatedAt = DateTime.UtcNow;
        _context.Institutions.Add(institution);
        
        try
        {
            await _context.SaveChangesAsync();
            
            // Invalidar caché
            await _cacheService.RemoveAsync("institutions_all");
            
            // Inicializar templates predefinidos para la nueva institución
            try
            {
                var templateInitializer = new CardTemplateInitializer(_context);
                await templateInitializer.InitializeDefaultTemplatesAsync(institution.Id);
            }
            catch (Exception ex)
            {
                // Log error pero no fallar la creación de la institución
                // Los templates pueden crearse manualmente después
                System.Diagnostics.Debug.WriteLine($"Warning: No se pudieron inicializar templates para la institución {institution.Name}: {ex.Message}");
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            if (pgEx.ConstraintName == "IX_Institutions_CardPrefix")
            {
                throw new InvalidOperationException(
                    $"El prefijo de carnet '{institution.CardPrefix}' ya está en uso. Por favor, elija otro prefijo.", ex);
            }
            throw;
        }
        
        return institution;
    }

    public async Task<Institution> UpdateAsync(Institution institution)
    {
        institution.UpdatedAt = DateTime.UtcNow;
        _context.Institutions.Update(institution);
        await _context.SaveChangesAsync();
        
        // Invalidar caché
        await _cacheService.RemoveAsync("institutions_all");
        await _cacheService.RemoveAsync($"institution_{institution.Id}");
        
        return institution;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return false;

        // VALIDACIONES: Verificar relaciones antes de eliminar
        var errors = new List<string>();

        // Verificar usuarios asociados (AppUser tiene InstitutionId nullable)
        var hasUsers = await _context.Set<Domain.Entities.AppUser>()
            .AnyAsync(u => u.InstitutionId == id);
        if (hasUsers)
        {
            errors.Add("usuarios");
        }

        // Verificar entidades asociadas
        var hasEntities = await _context.EntityProfiles
            .AnyAsync(e => e.InstitutionId == id);
        if (hasEntities)
        {
            errors.Add("entidades");
        }

        // Verificar carnets asociados
        var hasCards = await _context.Cards
            .AnyAsync(c => c.InstitutionId == id);
        if (hasCards)
        {
            errors.Add("carnets");
        }

        // Verificar eventos asociados
        var hasEvents = await _context.EventRecords
            .AnyAsync(e => e.InstitutionId == id);
        if (hasEvents)
        {
            errors.Add("eventos");
        }

        // Verificar plantillas de carnet asociadas
        var hasTemplates = await _context.CardTemplates
            .AnyAsync(t => t.InstitutionId == id);
        if (hasTemplates)
        {
            errors.Add("plantillas de carnet");
        }

        // Si hay relaciones, lanzar excepción con mensaje detallado
        if (errors.Count > 0)
        {
            var errorMessage = $"No se puede eliminar la institución porque tiene {string.Join(", ", errors)} asociados. " +
                              "Elimine primero los elementos relacionados.";
            throw new InvalidOperationException(errorMessage);
        }

        _context.Institutions.Remove(institution);
        await _context.SaveChangesAsync();
        
        // Invalidar caché
        await _cacheService.RemoveAsync("institutions_all");
        await _cacheService.RemoveAsync($"institution_{id}");
        
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return false;

        institution.IsActive = !institution.IsActive;
        institution.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        // Invalidar caché
        await _cacheService.RemoveAsync("institutions_all");
        await _cacheService.RemoveAsync($"institution_{id}");
        
        return true;
    }
}

