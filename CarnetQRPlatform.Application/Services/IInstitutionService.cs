using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface IInstitutionService
{
    Task<IEnumerable<Institution>> GetAllAsync();
    Task<Institution?> GetByIdAsync(Guid id);
    Task<Institution> CreateAsync(Institution institution);
    Task<Institution> UpdateAsync(Institution institution);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}

