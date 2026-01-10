using CarnetQRPlatform.Domain.Entities;

namespace CarnetQRPlatform.Web.Models;

public class HomeViewModel
{
    public bool IsSuperAdmin { get; set; }
    public int TotalInstitutions { get; set; }
    public int TotalEntities { get; set; }
    public int ActiveCards { get; set; }
    public int ScheduledEvents { get; set; }
    public double CompletionRate { get; set; }
    public List<EventRecord> RecentEvents { get; set; } = new();
    public List<Card> RecentCards { get; set; } = new();
    public List<EventRecord> UpcomingEvents { get; set; } = new();
}
