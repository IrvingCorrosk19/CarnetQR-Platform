using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class CardsController : Controller
{
    private readonly ICardService _cardService;
    private readonly ILogger<CardsController> _logger;
    private readonly QrCodeService _qrCodeService;

    public CardsController(ICardService cardService, ILogger<CardsController> logger, QrCodeService qrCodeService)
    {
        _cardService = cardService;
        _logger = logger;
        _qrCodeService = qrCodeService;
    }

    public async Task<IActionResult> Index()
    {
        var cards = await _cardService.GetAllAsync();
        return View(cards);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var card = await _cardService.GetByIdAsync(id);
        if (card == null)
        {
            return NotFound();
        }

        // Generar URL pública del QR
        var qrUrl = Url.Action("Show", "Qr", new { token = card.QrToken }, Request.Scheme) ?? 
                   $"{Request.Scheme}://{Request.Host}/q/{card.QrToken}";
        
        // Generar código QR en Base64
        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrUrl, size: 300);
        
        ViewBag.QrUrl = qrUrl;
        ViewBag.QrCodeBase64 = qrCodeBase64;

        return View(card);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var result = await _cardService.ToggleActiveAsync(id);
            if (!result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Carnet no encontrado." });
                }
                return NotFound();
            }

            var card = await _cardService.GetByIdAsync(id);
            var message = card?.IsActive == true ? "Carnet activado exitosamente." : "Carnet desactivado exitosamente.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = message, isActive = card?.IsActive });
            }

            TempData["SuccessMessage"] = message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado del carnet");
            var errorMsg = "Error al cambiar el estado del carnet.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}

