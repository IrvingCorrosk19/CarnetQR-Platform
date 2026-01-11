using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface IEntityProfileService
{
    Task<IEnumerable<EntityProfile>> GetAllAsync();
    Task<EntityProfile?> GetByIdAsync(Guid id);
    Task<EntityProfile> CreateAsync(EntityProfile entityProfile);
    Task<EntityProfile> UpdateAsync(EntityProfile entityProfile);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}

