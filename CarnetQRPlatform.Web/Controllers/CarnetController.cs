using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Web.Models;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class CarnetController : Controller
{
    private readonly ICardService _cardService;
    private readonly QrCodeService _qrCodeService;
    private readonly ILogger<CarnetController> _logger;

    public CarnetController(
        ICardService cardService,
        QrCodeService qrCodeService,
        ILogger<CarnetController> logger)
    {
        _cardService = cardService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Carnet/Print/{cardNumber}")]
    public async Task<IActionResult> Print(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
        {
            return NotFound();
        }

        // Buscar el carnet por número
        var allCards = await _cardService.GetAllAsync();
        var card = allCards.FirstOrDefault(c => c.CardNumber == cardNumber);

        if (card == null)
        {
            return NotFound();
        }

        // Generar URL del QR
        var qrUrl = Url.Action("Show", "Qr", new { token = card.QrToken }, Request.Scheme) ?? 
                   $"{Request.Scheme}://{Request.Host}/q/{card.QrToken}";
        
        // Generar QR en Base64
        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrUrl, size: 200);

        // Construir ViewModel
        var viewModel = new PrintCardViewModel
        {
            CardNumber = card.CardNumber,
            QrToken = card.QrToken,
            IssuedAt = card.IssuedAt,
            
            FirstName = card.EntityProfile?.FirstName ?? string.Empty,
            LastName = card.EntityProfile?.LastName ?? string.Empty,
            IdentificationNumber = card.EntityProfile?.IdentificationNumber,
            Email = card.EntityProfile?.Email,
            Phone = card.EntityProfile?.Phone,
            DateOfBirth = card.EntityProfile?.DateOfBirth,
            PhotoPath = card.EntityProfile?.PhotoPath,
            
            InstitutionName = card.Institution?.Name ?? string.Empty,
            InstitutionLogoPath = card.Institution?.LogoPath,
            
            QrCodeBase64 = qrCodeBase64,
            
            Config = new PrintCardConfig
            {
                // Tamaño estándar de tarjeta (85.6mm x 54mm)
                Width = 85.6,
                Height = 54.0,
                Orientation = "horizontal",
                
                // Elementos visibles por defecto
                ShowLogo = !string.IsNullOrEmpty(card.Institution?.LogoPath),
                ShowInstitutionName = true,
                ShowUserName = true,
                ShowCardNumber = true,
                ShowQrCode = true,
                ShowPhoto = card.Institution?.PhotoEnabled == true && !string.IsNullOrEmpty(card.EntityProfile?.PhotoPath),
                ShowIdentificationNumber = false,
                ShowEmail = false,
                ShowPhone = false
            }
        };

        // Permitir personalización vía query string
        if (Request.Query.ContainsKey("width"))
        {
            if (double.TryParse(Request.Query["width"], out var width))
                viewModel.Config.Width = width;
        }
        
        if (Request.Query.ContainsKey("height"))
        {
            if (double.TryParse(Request.Query["height"], out var height))
                viewModel.Config.Height = height;
        }
        
        if (Request.Query.ContainsKey("orientation"))
        {
            viewModel.Config.Orientation = Request.Query["orientation"].ToString().ToLower();
        }
        
        if (Request.Query.ContainsKey("showLogo"))
        {
            if (bool.TryParse(Request.Query["showLogo"], out var showLogo))
                viewModel.Config.ShowLogo = showLogo;
        }
        
        if (Request.Query.ContainsKey("showInstitutionName"))
        {
            if (bool.TryParse(Request.Query["showInstitutionName"], out var showInstitutionName))
                viewModel.Config.ShowInstitutionName = showInstitutionName;
        }
        
        if (Request.Query.ContainsKey("showUserName"))
        {
            if (bool.TryParse(Request.Query["showUserName"], out var showUserName))
                viewModel.Config.ShowUserName = showUserName;
        }
        
        if (Request.Query.ContainsKey("showCardNumber"))
        {
            if (bool.TryParse(Request.Query["showCardNumber"], out var showCardNumber))
                viewModel.Config.ShowCardNumber = showCardNumber;
        }
        
        if (Request.Query.ContainsKey("showQrCode"))
        {
            if (bool.TryParse(Request.Query["showQrCode"], out var showQrCode))
                viewModel.Config.ShowQrCode = showQrCode;
        }
        
        if (Request.Query.ContainsKey("showIdentificationNumber"))
        {
            if (bool.TryParse(Request.Query["showIdentificationNumber"], out var showId))
                viewModel.Config.ShowIdentificationNumber = showId;
        }
        
        if (Request.Query.ContainsKey("showEmail"))
        {
            if (bool.TryParse(Request.Query["showEmail"], out var showEmail))
                viewModel.Config.ShowEmail = showEmail;
        }
        
        if (Request.Query.ContainsKey("showPhone"))
        {
            if (bool.TryParse(Request.Query["showPhone"], out var showPhone))
                viewModel.Config.ShowPhone = showPhone;
        }
        
        if (Request.Query.ContainsKey("showPhoto"))
        {
            if (bool.TryParse(Request.Query["showPhoto"], out var showPhoto))
                viewModel.Config.ShowPhoto = showPhoto;
        }

        return View("PrintCarnet", viewModel);
    }
}

