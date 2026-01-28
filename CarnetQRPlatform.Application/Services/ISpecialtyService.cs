using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface ISpecialtyService
{
    Task<IEnumerable<Specialty>> GetAllAsync();
    Task<Specialty?> GetByIdAsync(Guid id);
    Task<Specialty> CreateAsync(Specialty specialty);
    Task<Specialty> UpdateAsync(Specialty specialty);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}
