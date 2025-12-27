using CarnetQRPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "InstitutionAdminOrAbove")]
public class StatisticsController : Controller
{
    private readonly IEventService _eventService;
    private readonly ICardService _cardService;
    private readonly IEntityProfileService _entityProfileService;

    public StatisticsController(
        IEventService eventService,
        ICardService cardService,
        IEntityProfileService entityProfileService)
    {
        _eventService = eventService;
        _cardService = cardService;
        _entityProfileService = entityProfileService;
    }

    public async Task<IActionResult> Index(string period = "month")
    {
        var events = await _eventService.GetAllAsync();
        var cards = await _cardService.GetAllAsync();
        var entities = await _entityProfileService.GetAllAsync();

        // Estadísticas generales
        var totalScheduled = events.Count(e => e.Status == Domain.Entities.EventStatus.Scheduled);
        var totalCompleted = events.Count(e => e.Status == Domain.Entities.EventStatus.Completed);
        var totalNotCompleted = events.Count(e => e.Status == Domain.Entities.EventStatus.NotCompleted);
        var totalAttended = totalCompleted + totalNotCompleted;
        var completionRate = totalAttended > 0
            ? (totalCompleted * 100.0) / totalAttended
            : 0;

        // Calcular período para tendencias
        DateTime startDate;
        switch (period.ToLower())
        {
            case "week":
                startDate = DateTime.UtcNow.AddDays(-7);
                break;
            case "month":
                startDate = DateTime.UtcNow.AddMonths(-1);
                break;
            case "quarter":
                startDate = DateTime.UtcNow.AddMonths(-3);
                break;
            case "year":
                startDate = DateTime.UtcNow.AddYears(-1);
                break;
            default:
                startDate = DateTime.UtcNow.AddMonths(-1);
                break;
        }

        // Tendencias por período
        var periodEvents = events.Where(e => e.ScheduledAt >= startDate).ToList();
        var periodScheduled = periodEvents.Count(e => e.Status == Domain.Entities.EventStatus.Scheduled);
        var periodCompleted = periodEvents.Count(e => e.Status == Domain.Entities.EventStatus.Completed);
        var periodNotCompleted = periodEvents.Count(e => e.Status == Domain.Entities.EventStatus.NotCompleted);
        var periodAttended = periodCompleted + periodNotCompleted;
        var periodCompletionRate = periodAttended > 0
            ? (periodCompleted * 100.0) / periodAttended
            : 0;

        // Agrupar por día para gráfica de tendencias
        var dailyTrends = periodEvents
            .Where(e => e.ScheduledAt >= startDate)
            .GroupBy(e => e.ScheduledAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Scheduled = g.Count(e => e.Status == Domain.Entities.EventStatus.Scheduled),
                Completed = g.Count(e => e.Status == Domain.Entities.EventStatus.Completed),
                NotCompleted = g.Count(e => e.Status == Domain.Entities.EventStatus.NotCompleted)
            })
            .OrderBy(x => x.Date)
            .ToList();

        ViewBag.TotalScheduled = totalScheduled;
        ViewBag.TotalCompleted = totalCompleted;
        ViewBag.TotalNotCompleted = totalNotCompleted;
        ViewBag.CompletionRate = Math.Round(completionRate, 2);
        ViewBag.TotalCards = cards.Count();
        ViewBag.TotalEntities = entities.Count();
        
        ViewBag.Period = period;
        ViewBag.PeriodScheduled = periodScheduled;
        ViewBag.PeriodCompleted = periodCompleted;
        ViewBag.PeriodNotCompleted = periodNotCompleted;
        ViewBag.PeriodCompletionRate = Math.Round(periodCompletionRate, 2);
        ViewBag.DailyTrends = dailyTrends;

        return View();
    }
}

