using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

public class InstitutionService : IInstitutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public InstitutionService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<Institution>> GetAllAsync()
    {
        return await _context.Institutions
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<Institution?> GetByIdAsync(Guid id)
    {
        return await _context.Institutions.FindAsync(id);
    }

    public async Task<Institution> CreateAsync(Institution institution)
    {
        institution.Id = Guid.NewGuid();
        institution.CreatedAt = DateTime.UtcNow;
        _context.Institutions.Add(institution);
        await _context.SaveChangesAsync();
        return institution;
    }

    public async Task<Institution> UpdateAsync(Institution institution)
    {
        institution.UpdatedAt = DateTime.UtcNow;
        _context.Institutions.Update(institution);
        await _context.SaveChangesAsync();
        return institution;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return false;

        _context.Institutions.Remove(institution);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return false;

        institution.IsActive = !institution.IsActive;
        institution.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}

