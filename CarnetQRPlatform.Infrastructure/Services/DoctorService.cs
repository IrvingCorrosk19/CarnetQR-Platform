using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Application.Common;
using CarnetQRPlatform.Application.Extensions;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public DoctorService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        var query = _context.Set<Doctor>()
            .Include(d => d.Specialty)
            .Include(d => d.Institution)
            .AsQueryable();
        
        return await query
            .ApplyTenantFilter(_tenantProvider)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task<PagedResult<Doctor>> GetAllPagedAsync(PaginationParameters parameters)
    {
        var query = _context.Set<Doctor>()
            .Include(d => d.Specialty)
            .Include(d => d.Institution)
            .AsQueryable();
        
        return await query
            .ApplyTenantFilter(_tenantProvider)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToPagedResultAsync(parameters);
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        var query = _context.Set<Doctor>()
            .Include(d => d.Specialty)
            .Include(d => d.Institution)
            .AsQueryable();
        
        return await query
            .ApplyTenantFilter(_tenantProvider)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Iniciando creación. Nombre: {doctor.FirstName} {doctor.LastName}, InstitutionId: {doctor.InstitutionId}, SpecialtyId: {doctor.SpecialtyId}");
        
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var isSuperAdmin = _tenantProvider.IsSuperAdmin();
        
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] TenantId: {tenantId}, IsSuperAdmin: {isSuperAdmin}");
        
        // Si es SuperAdmin, debe proporcionar InstitutionId explícitamente
        if (isSuperAdmin)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Usuario es SuperAdmin. Validando InstitutionId: {doctor.InstitutionId}");
            if (doctor.InstitutionId == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ERROR: SuperAdmin no proporcionó InstitutionId");
                throw new InvalidOperationException("SuperAdmin must specify InstitutionId when creating doctors");
            }
        }
        else
        {
            // Si no hay tenant y no es SuperAdmin, rechazar
            if (!tenantId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ERROR: No hay tenant context");
                throw new InvalidOperationException("Cannot create doctor without tenant context");
            }
            
            // Para usuarios no-SuperAdmin, FORZAR InstitutionId desde el tenant
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Forzando InstitutionId desde tenant: {tenantId.Value}");
            doctor.InstitutionId = tenantId.Value;
        }

        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Validando especialidad. SpecialtyId: {doctor.SpecialtyId}");
        // Validar que la especialidad existe
        var specialty = await _context.Set<Specialty>()
            .FirstOrDefaultAsync(s => s.Id == doctor.SpecialtyId);
        
        if (specialty == null || !specialty.IsActive)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ERROR: Especialidad no existe o está inactiva. SpecialtyId: {doctor.SpecialtyId}, Found: {specialty != null}, IsActive: {specialty?.IsActive}");
            throw new InvalidOperationException("La especialidad seleccionada no existe o está inactiva.");
        }
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Especialidad válida: {specialty.Name}");

        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Validando institución. InstitutionId: {doctor.InstitutionId}");
        // Validar que la institución existe
        var institution = await _context.Institutions
            .FirstOrDefaultAsync(i => i.Id == doctor.InstitutionId);
        
        if (institution == null)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ERROR: Institución no existe. InstitutionId: {doctor.InstitutionId}");
            throw new InvalidOperationException("La institución seleccionada no existe.");
        }
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Institución válida: {institution.Name}");

        doctor.Id = Guid.NewGuid();
        doctor.CreatedAt = DateTime.UtcNow;
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ID generado: {doctor.Id}, CreatedAt: {doctor.CreatedAt}");
        
        _context.Set<Doctor>().Add(doctor);
        System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Médico agregado al contexto");
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Guardando cambios en la base de datos...");
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] Cambios guardados exitosamente. ID: {doctor.Id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] ERROR INESPERADO: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DoctorService.CreateAsync] InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            throw;
        }
        
        return doctor;
    }

    public async Task<Doctor> UpdateAsync(Doctor doctor)
    {
        // MULTI-TENANT ESTRICTO: GetByIdAsync ya aplica filtro de tenant
        var existingDoctor = await GetByIdAsync(doctor.Id);
        if (existingDoctor == null)
        {
            throw new ArgumentException("Doctor not found or access denied");
        }

        // Validar que la especialidad existe
        var specialty = await _context.Set<Specialty>()
            .FirstOrDefaultAsync(s => s.Id == doctor.SpecialtyId);
        
        if (specialty == null || !specialty.IsActive)
        {
            throw new InvalidOperationException("La especialidad seleccionada no existe o está inactiva.");
        }

        // Actualizar solo campos permitidos (InstitutionId se preserva, no se puede cambiar)
        existingDoctor.SpecialtyId = doctor.SpecialtyId;
        existingDoctor.FirstName = doctor.FirstName;
        existingDoctor.LastName = doctor.LastName;
        existingDoctor.Email = doctor.Email;
        existingDoctor.Phone = doctor.Phone;
        existingDoctor.LicenseNumber = doctor.LicenseNumber;
        existingDoctor.IsActive = doctor.IsActive;
        existingDoctor.UpdatedAt = DateTime.UtcNow;
        
        _context.Set<Doctor>().Update(existingDoctor);
        await _context.SaveChangesAsync();
        
        return existingDoctor;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var doctor = await GetByIdAsync(id);
        if (doctor == null) return false;

        _context.Set<Doctor>().Remove(doctor);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var doctor = await GetByIdAsync(id);
        if (doctor == null) return false;

        doctor.IsActive = !doctor.IsActive;
        doctor.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return true;
    }
}
