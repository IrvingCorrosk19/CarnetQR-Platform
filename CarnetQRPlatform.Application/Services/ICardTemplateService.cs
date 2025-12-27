using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface ICardTemplateService
{
    Task<IEnumerable<CardTemplate>> GetAllAsync();
    Task<CardTemplate?> GetByIdAsync(Guid id);
    Task<CardTemplate?> GetDefaultTemplateAsync();
    Task<CardTemplate> CreateAsync(CardTemplate template);
    Task<CardTemplate> UpdateAsync(CardTemplate template);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SetAsDefaultAsync(Guid id);
}


