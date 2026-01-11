using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class InstitutionTypeService : IInstitutionTypeService
{
    private readonly ApplicationDbContext _context;

    public InstitutionTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InstitutionType>> GetAllAsync()
    {
        return await _context.InstitutionTypes
            .OrderBy(it => it.Name)
            .ToListAsync();
    }

    public async Task<InstitutionType?> GetByIdAsync(Guid id)
    {
        return await _context.InstitutionTypes.FindAsync(id);
    }

    public async Task<InstitutionType> CreateAsync(InstitutionType institutionType)
    {
        // Verificar si el nombre ya existe
        var existing = await _context.InstitutionTypes
            .FirstOrDefaultAsync(it => it.Name == institutionType.Name);
        
        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Ya existe un tipo de institución con el nombre '{institutionType.Name}'.");
        }
        
        institutionType.Id = Guid.NewGuid();
        institutionType.CreatedAt = DateTime.UtcNow;
        _context.InstitutionTypes.Add(institutionType);
        await _context.SaveChangesAsync();
        
        return institutionType;
    }

    public async Task<InstitutionType> UpdateAsync(InstitutionType institutionType)
    {
        var existing = await GetByIdAsync(institutionType.Id);
        if (existing == null)
        {
            throw new ArgumentException("Tipo de institución no encontrado");
        }

        // Verificar si el nombre ya existe en otro tipo
        var duplicate = await _context.InstitutionTypes
            .FirstOrDefaultAsync(it => it.Name == institutionType.Name && it.Id != institutionType.Id);
        
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"Ya existe un tipo de institución con el nombre '{institutionType.Name}'.");
        }

        existing.Name = institutionType.Name;
        existing.Description = institutionType.Description;
        existing.IsActive = institutionType.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        
        _context.InstitutionTypes.Update(existing);
        await _context.SaveChangesAsync();
        
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var institutionType = await GetByIdAsync(id);
        if (institutionType == null) return false;

        // VALIDACIÓN: Verificar si está siendo usado por alguna institución
        var hasInstitutions = await _context.Institutions
            .AnyAsync(i => i.InstitutionTypeId == id);
        
        if (hasInstitutions)
        {
            throw new InvalidOperationException(
                "No se puede eliminar el tipo de institución porque está siendo usado por una o más instituciones. " +
                "Elimine o cambie el tipo de las instituciones relacionadas primero.");
        }

        _context.InstitutionTypes.Remove(institutionType);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var institutionType = await GetByIdAsync(id);
        if (institutionType == null) return false;

        institutionType.IsActive = !institutionType.IsActive;
        institutionType.UpdatedAt = DateTime.UtcNow;
        _context.InstitutionTypes.Update(institutionType);
        await _context.SaveChangesAsync();
        
        return true;
    }
}

