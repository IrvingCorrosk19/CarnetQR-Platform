using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface IInstitutionTypeService
{
    Task<IEnumerable<InstitutionType>> GetAllAsync();
    Task<InstitutionType?> GetByIdAsync(Guid id);
    Task<InstitutionType> CreateAsync(InstitutionType institutionType);
    Task<InstitutionType> UpdateAsync(InstitutionType institutionType);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}

