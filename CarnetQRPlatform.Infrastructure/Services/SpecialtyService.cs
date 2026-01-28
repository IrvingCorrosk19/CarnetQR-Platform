using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Application.Extensions;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ITenantProvider _tenantProvider;

    public SpecialtyService(ApplicationDbContext context, ICacheService cacheService, ITenantProvider tenantProvider)
    {
        _context = context;
        _cacheService = cacheService;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<Specialty>> GetAllAsync()
    {
        var query = _context.Set<Specialty>()
            .Include(s => s.Institution)
            .AsQueryable();
        
        // Aplicar filtro de tenant
        query = query.ApplyTenantFilter(_tenantProvider);
        
        var specialties = await query
            .OrderBy(s => s.Name)
            .ToListAsync();
        
        return specialties;
    }

    public async Task<Specialty?> GetByIdAsync(Guid id)
    {
        var query = _context.Set<Specialty>()
            .Include(s => s.Institution)
            .AsQueryable();
        
        // Aplicar filtro de tenant
        query = query.ApplyTenantFilter(_tenantProvider);
        
        var specialty = await query.FirstOrDefaultAsync(s => s.Id == id);
        
        return specialty;
    }

    public async Task<Specialty> CreateAsync(Specialty specialty)
    {
        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Iniciando creación. Nombre: {specialty.Name}, InstitutionId: {specialty.InstitutionId}");
        
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var isSuperAdmin = _tenantProvider.IsSuperAdmin();
        
        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] TenantId: {tenantId}, IsSuperAdmin: {isSuperAdmin}");
        
        // Si es SuperAdmin, debe proporcionar InstitutionId explícitamente
        if (isSuperAdmin)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Usuario es SuperAdmin. Validando InstitutionId: {specialty.InstitutionId}");
            if (specialty.InstitutionId == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR: SuperAdmin no proporcionó InstitutionId");
                throw new InvalidOperationException("SuperAdmin must specify InstitutionId when creating specialties");
            }
        }
        else
        {
            // Si no hay tenant y no es SuperAdmin, rechazar
            if (!tenantId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR: No hay tenant context");
                throw new InvalidOperationException("Cannot create specialty without tenant context");
            }
            
            // Para usuarios no-SuperAdmin, FORZAR InstitutionId desde el tenant
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Forzando InstitutionId desde tenant: {tenantId.Value}");
            specialty.InstitutionId = tenantId.Value;
        }

        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Validando institución. InstitutionId: {specialty.InstitutionId}");
        // Validar que la institución existe
        var institution = await _context.Institutions
            .FirstOrDefaultAsync(i => i.Id == specialty.InstitutionId);
        
        if (institution == null)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR: Institución no existe. InstitutionId: {specialty.InstitutionId}");
            throw new InvalidOperationException("La institución seleccionada no existe.");
        }
        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Institución válida: {institution.Name}");
        
        // Verificar si el nombre ya existe en la misma institución
        var existingSpecialty = await _context.Set<Specialty>()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == specialty.Name.ToLower() && s.InstitutionId == specialty.InstitutionId);
        
        if (existingSpecialty != null)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR: La especialidad '{specialty.Name}' ya existe en esta institución.");
            throw new InvalidOperationException(
                $"La especialidad '{specialty.Name}' ya existe en esta institución.");
        }
        
        specialty.Id = Guid.NewGuid();
        specialty.CreatedAt = DateTime.UtcNow;
        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ID generado: {specialty.Id}, CreatedAt: {specialty.CreatedAt}");
        
        _context.Set<Specialty>().Add(specialty);
        System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Especialidad agregada al contexto");
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Guardando cambios en la base de datos...");
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] Cambios guardados exitosamente. ID: {specialty.Id}");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR DB: Violación de constraint único. SqlState: {pgEx.SqlState}, ConstraintName: {pgEx.ConstraintName}");
            if (pgEx.ConstraintName?.Contains("Name") == true || pgEx.ConstraintName?.Contains("InstitutionId") == true)
            {
                throw new InvalidOperationException(
                    $"La especialidad '{specialty.Name}' ya existe en esta institución. Por favor, elija otro nombre.", ex);
            }
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] ERROR INESPERADO: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SpecialtyService.CreateAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
        
        return specialty;
    }

    public async Task<Specialty> UpdateAsync(Specialty specialty)
    {
        // MULTI-TENANT ESTRICTO: GetByIdAsync ya aplica filtro de tenant
        var existingSpecialty = await GetByIdAsync(specialty.Id);
        if (existingSpecialty == null)
        {
            throw new ArgumentException("Specialty not found or access denied");
        }

        // Verificar si el nombre ya existe en otra especialidad de la misma institución
        var duplicateSpecialty = await _context.Set<Specialty>()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == specialty.Name.ToLower() 
                && s.Id != specialty.Id 
                && s.InstitutionId == existingSpecialty.InstitutionId);
        
        if (duplicateSpecialty != null)
        {
            throw new InvalidOperationException(
                $"La especialidad '{specialty.Name}' ya existe en esta institución.");
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva, no se puede cambiar)
        existingSpecialty.Name = specialty.Name;
        existingSpecialty.Description = specialty.Description;
        existingSpecialty.IsActive = specialty.IsActive;
        existingSpecialty.UpdatedAt = DateTime.UtcNow;
        
        _context.Set<Specialty>().Update(existingSpecialty);
        await _context.SaveChangesAsync();
        
        return existingSpecialty;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var specialty = await _context.Set<Specialty>().FindAsync(id);
        if (specialty == null) return false;

        // Verificar si hay médicos asociados
        var hasDoctors = await _context.Set<Doctor>()
            .AnyAsync(d => d.SpecialtyId == id);
        if (hasDoctors)
        {
            throw new InvalidOperationException(
                "No se puede eliminar la especialidad porque tiene médicos asociados. " +
                "Elimine primero los médicos relacionados.");
        }

        _context.Set<Specialty>().Remove(specialty);
        await _context.SaveChangesAsync();
        
        await _cacheService.RemoveAsync("specialties_all");
        await _cacheService.RemoveAsync($"specialty_{id}");
        
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var specialty = await _context.Set<Specialty>().FindAsync(id);
        if (specialty == null) return false;

        specialty.IsActive = !specialty.IsActive;
        specialty.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        await _cacheService.RemoveAsync("specialties_all");
        await _cacheService.RemoveAsync($"specialty_{id}");
        
        return true;
    }
}
