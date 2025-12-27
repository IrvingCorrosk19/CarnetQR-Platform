using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Web.Controllers;

[AllowAnonymous]
public class QrController : Controller
{
    private readonly ICardService _cardService;
    private readonly IEventService _eventService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QrController> _logger;
    private readonly QrCodeService _qrCodeService;

    public QrController(ICardService cardService, IEventService eventService, ApplicationDbContext context, ILogger<QrController> logger, QrCodeService qrCodeService)
    {
        _cardService = cardService;
        _eventService = eventService;
        _context = context;
        _logger = logger;
        _qrCodeService = qrCodeService;
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

        // Obtener la institución con su configuración
        var institution = await _context.Institutions
            .FirstOrDefaultAsync(i => i.Id == card.InstitutionId);

        ViewBag.UpcomingEvents = upcoming;
        ViewBag.HistoryEvents = history;
        ViewBag.Institution = institution;

        // Determinar qué mostrar según configuración
        var displayMode = institution?.QrPublicDisplayMode ?? Domain.Entities.QrPublicDisplayMode.CardNumber;
        ViewBag.DisplayIdentifier = displayMode == Domain.Entities.QrPublicDisplayMode.PatientName
            ? $"{card.EntityProfile?.FirstName} {card.EntityProfile?.LastName}"
            : card.CardNumber;

        // Generar código QR para mostrar en la vista pública
        var qrUrl = Url.Action("Show", "Qr", new { token = card.QrToken }, Request.Scheme) ?? 
                   $"{Request.Scheme}://{Request.Host}/q/{card.QrToken}";
        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrUrl, size: 250);
        
        ViewBag.QrUrl = qrUrl;
        ViewBag.QrCodeBase64 = qrCodeBase64;

        return View(card);
    }
}

