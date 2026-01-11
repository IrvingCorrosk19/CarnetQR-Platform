using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Application.Services;

public interface IEventService
{
    Task<IEnumerable<EventRecord>> GetAllAsync();
    Task<IEnumerable<EventRecord>> GetUpcomingAsync();
    Task<IEnumerable<EventRecord>> GetByEntityProfileAsync(Guid entityProfileId);
    Task<EventRecord?> GetByIdAsync(Guid id);
    Task<EventRecord> CreateAsync(EventRecord eventRecord);
    Task<EventRecord> UpdateAsync(EventRecord eventRecord);
    Task<EventRecord> UpdateStatusAsync(Guid id, EventStatus status);
    Task<bool> DeleteAsync(Guid id);
}

