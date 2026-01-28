using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Application.Common;

namespace CarnetQRPlatform.Application.Services;

public interface IDoctorService
{
    Task<IEnumerable<Doctor>> GetAllAsync();
    Task<PagedResult<Doctor>> GetAllPagedAsync(PaginationParameters parameters);
    Task<Doctor?> GetByIdAsync(Guid id);
    Task<Doctor> CreateAsync(Doctor doctor);
    Task<Doctor> UpdateAsync(Doctor doctor);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}
