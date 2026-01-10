using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

        // Cargar eventos relacionados con la entidad, filtrando por la institución del carnet para seguridad
        // (en endpoint público no hay tenant, pero debemos asegurar que solo vea eventos de la misma institución)
        var allEvents = await _eventService.GetByEntityProfileAsync(card.EntityProfileId);
        // Filtrar adicionalmente por InstitutionId del card para seguridad en endpoint público
        var eventsList = allEvents
            .Where(e => e.InstitutionId == card.InstitutionId)
            .OrderByDescending(e => e.ScheduledAt)
            .ToList();

        // Obtener la institución con su configuración
        var institution = await _context.Institutions
            .FirstOrDefaultAsync(i => i.Id == card.InstitutionId);

        ViewBag.AllEvents = eventsList;
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

