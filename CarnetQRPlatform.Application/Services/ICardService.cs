using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Application.Common;

namespace CarnetQRPlatform.Application.Services;

public interface ICardService
{
    Task<IEnumerable<Card>> GetAllAsync();
    Task<PagedResult<Card>> GetAllPagedAsync(PaginationParameters parameters);
    Task<Card?> GetByIdAsync(Guid id);
    Task<Card?> GetByQrTokenAsync(string qrToken);
    Task<Card> CreateAsync(Guid entityProfileId);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleActiveAsync(Guid id);
}

