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

    public async Task<IActionResult> Index()
    {
        var events = await _eventService.GetAllAsync();
        var cards = await _cardService.GetAllAsync();
        var entities = await _entityProfileService.GetAllAsync();

        var totalScheduled = events.Count(e => e.Status == Domain.Entities.EventStatus.Scheduled);
        var totalCompleted = events.Count(e => e.Status == Domain.Entities.EventStatus.Completed);
        var totalNotCompleted = events.Count(e => e.Status == Domain.Entities.EventStatus.NotCompleted);
        var completionRate = totalScheduled + totalCompleted + totalNotCompleted > 0
            ? (totalCompleted * 100.0) / (totalCompleted + totalNotCompleted)
            : 0;

        ViewBag.TotalScheduled = totalScheduled;
        ViewBag.TotalCompleted = totalCompleted;
        ViewBag.TotalNotCompleted = totalNotCompleted;
        ViewBag.CompletionRate = completionRate;
        ViewBag.TotalCards = cards.Count();
        ViewBag.TotalEntities = entities.Count();

        return View();
    }
}

