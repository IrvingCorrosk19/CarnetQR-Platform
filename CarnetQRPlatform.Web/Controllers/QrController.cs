using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Web.Controllers;

[AllowAnonymous]
public class QrController : Controller
{
    private readonly ICardService _cardService;
    private readonly IEventService _eventService;
    private readonly ICardTemplateService _templateService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QrController> _logger;

    public QrController(ICardService cardService, IEventService eventService, ICardTemplateService templateService, ApplicationDbContext context, ILogger<QrController> logger)
    {
        _cardService = cardService;
        _eventService = eventService;
        _templateService = templateService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Route("/q/{token}")]
    public async Task<IActionResult> Show(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return NotFound();
        }

        var card = await _cardService.GetByQrTokenAsync(token);
        if (card == null || !card.IsActive)
        {
            return NotFound();
        }

        var upcomingEvents = await _eventService.GetByEntityProfileAsync(card.EntityProfileId);
        var upcoming = upcomingEvents.Where(e => e.ScheduledAt >= DateTime.UtcNow && e.Status == Domain.Entities.EventStatus.Scheduled)
            .OrderBy(e => e.ScheduledAt)
            .Take(5);

        var history = upcomingEvents.Where(e => e.ScheduledAt < DateTime.UtcNow || e.Status != Domain.Entities.EventStatus.Scheduled)
            .OrderByDescending(e => e.ScheduledAt)
            .Take(10);

        // Obtener la plantilla de la institución del card (sin filtro de tenant para endpoint público)
        var template = await _context.CardTemplates
            .Include(t => t.Institution)
            .Where(t => t.InstitutionId == card.InstitutionId)
            .OrderByDescending(t => t.IsDefault)
            .FirstOrDefaultAsync();

        ViewBag.UpcomingEvents = upcoming;
        ViewBag.HistoryEvents = history;
        ViewBag.Template = template;

        return View(card);
    }
}

