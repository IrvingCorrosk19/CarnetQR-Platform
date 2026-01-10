using System.Text.Json;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Web.Models;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class CarnetController : Controller
{
    private readonly ICardService _cardService;
    private readonly ICardTemplateService _cardTemplateService;
    private readonly QrCodeService _qrCodeService;
    private readonly ILogger<CarnetController> _logger;

    public CarnetController(
        ICardService cardService,
        ICardTemplateService cardTemplateService,
        QrCodeService qrCodeService,
        ILogger<CarnetController> logger)
    {
        _cardService = cardService;
        _cardTemplateService = cardTemplateService;
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

        // Cargar template si existe (por defecto o específico vía query string)
        CardTemplate? template = null;
        if (Request.Query.ContainsKey("templateId"))
        {
            if (Guid.TryParse(Request.Query["templateId"], out var templateId))
            {
                template = await _cardTemplateService.GetByIdAsync(templateId);
            }
        }
        else
        {
            // Cargar template por defecto de la institución
            template = await _cardTemplateService.GetDefaultTemplateAsync();
        }

        // Verificar si hay foto disponible
        var hasPhoto = !string.IsNullOrEmpty(card.EntityProfile?.PhotoPath);
        
        // Crear configuración inicial con valores por defecto
        var config = new PrintCardConfig();
        
        // CONFIGURACIÓN POR DEFECTO PRIORITARIA: foto en frente, QR en trasera (si hay foto)
        // Esta se aplica primero para que tenga prioridad sobre el template
        if (hasPhoto)
        {
            config.ShowPhoto = true;
            config.DoubleSided = true;
            config.QrOnBack = true;
            config.ShowQrCode = false; // No mostrar QR en el frente (va en la trasera)
        }
        
        // Aplicar configuraciones desde template si existe (puede sobrescribir algunas, pero respetamos la prioridad de foto/QR)
        if (template != null && template.TemplateConfig != null && template.TemplateConfig.Count > 0)
        {
            ApplyTemplateConfig(config, template.TemplateConfig);
            
            // Si hay foto, forzar que se muestre y el QR vaya en la trasera (prioridad sobre template)
            if (hasPhoto)
            {
                config.ShowPhoto = true;
                config.DoubleSided = true;
                config.QrOnBack = true;
                config.ShowQrCode = false;
            }
        }
        
        if (card.Institution != null)
        {
            config.ShowLogo = !string.IsNullOrEmpty(card.Institution.LogoPath);
            // Si hay campos visibles configurados en la institución, aplicarlos
            if (card.Institution.VisibleFields != null && card.Institution.VisibleFields.Any())
            {
                ApplyInstitutionVisibleFields(config, card.Institution.VisibleFields);
            }
        }

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
            
            Config = config
        };

        // Permitir override vía query string (sobrescribe configuraciones de template y por defecto)
        ApplyQueryStringOverrides(viewModel.Config);
        
        // VERIFICACIÓN FINAL Y ASEGURAR COHERENCIA:
        // Si hay foto, priorizar configuración: foto en frente, QR en trasera
        if (!string.IsNullOrEmpty(viewModel.PhotoPath))
        {
            viewModel.Config.ShowPhoto = true;
            
            // Asegurar que si hay foto, por defecto el QR vaya en la trasera
            // (solo si no se especificó explícitamente via query string que no debe estar en la trasera)
            if (!Request.Query.ContainsKey("qrOnBack") && !Request.Query.ContainsKey("doubleSided"))
            {
                viewModel.Config.DoubleSided = true;
                viewModel.Config.QrOnBack = true;
                viewModel.Config.ShowQrCode = false; // No mostrar QR en el frente
            }
        }
        
        // Asegurar coherencia general: si QrOnBack está activo, el QR no debe mostrarse en el frente
        if (viewModel.Config.QrOnBack)
        {
            viewModel.Config.DoubleSided = true;
            viewModel.Config.ShowQrCode = false;
        }
        
        // Si DoubleSided está activo pero QrOnBack no está explícitamente desactivado, activarlo por defecto
        if (viewModel.Config.DoubleSided && !Request.Query.ContainsKey("qrOnBack") && !string.IsNullOrEmpty(viewModel.PhotoPath))
        {
            viewModel.Config.QrOnBack = true;
            viewModel.Config.ShowQrCode = false;
        }

        return View("PrintCarnet", viewModel);
    }

    /// <summary>
    /// Aplica configuraciones desde TemplateConfig (Dictionary) a PrintCardConfig
    /// </summary>
    private void ApplyTemplateConfig(PrintCardConfig config, Dictionary<string, object> templateConfig)
    {
        foreach (var kvp in templateConfig)
        {
            try
            {
                var property = typeof(PrintCardConfig).GetProperty(kvp.Key, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                
                if (property != null && property.CanWrite)
                {
                    var value = ConvertValue(kvp.Value, property.PropertyType);
                    if (value != null)
                    {
                        property.SetValue(config, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error aplicando configuración de template: {Key} = {Value}", kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Aplica campos visibles desde la configuración de institución
    /// </summary>
    private void ApplyInstitutionVisibleFields(PrintCardConfig config, List<string> visibleFields)
    {
        // Resetear todos los campos a false primero
        config.ShowIdentificationNumber = false;
        config.ShowEmail = false;
        config.ShowPhone = false;
        config.ShowDateOfBirth = false;

        foreach (var field in visibleFields)
        {
            switch (field.ToLower())
            {
                case "identificationnumber":
                case "identification":
                case "id":
                    config.ShowIdentificationNumber = true;
                    break;
                case "email":
                    config.ShowEmail = true;
                    break;
                case "phone":
                case "telephone":
                    config.ShowPhone = true;
                    break;
                case "dateofbirth":
                case "dob":
                case "birthdate":
                    config.ShowDateOfBirth = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Convierte un valor a un tipo específico
    /// </summary>
    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null) return null;
        
        var sourceType = value.GetType();
        
        // Si ya es del tipo correcto, retornar directamente
        if (targetType.IsAssignableFrom(sourceType))
        {
            return value;
        }

        // Manejar conversiones comunes
        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            if (double.TryParse(value.ToString(), out var d))
                return d;
        }
        else if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            if (bool.TryParse(value.ToString(), out var b))
                return b;
        }
        else if (targetType == typeof(int) || targetType == typeof(int?))
        {
            if (int.TryParse(value.ToString(), out var i))
                return i;
        }
        else if (targetType == typeof(string))
        {
            return value.ToString();
        }
        else if (targetType == typeof(Dictionary<string, string>))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, string>();
                foreach (var prop in jsonElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ToString();
                }
                return dict;
            }
            if (value is Dictionary<string, object> dictObj)
            {
                return dictObj.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty);
            }
        }
        else if (targetType == typeof(List<string>))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray().Select(e => e.ToString()).ToList();
            }
            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.Select(o => o?.ToString() ?? string.Empty).ToList();
            }
        }

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// Aplica sobreescrituras desde query string
    /// </summary>
    private void ApplyQueryStringOverrides(PrintCardConfig config)
    {
        // Tamaño y orientación
        if (Request.Query.ContainsKey("width") && double.TryParse(Request.Query["width"], out var width))
            config.Width = width;
        
        if (Request.Query.ContainsKey("height") && double.TryParse(Request.Query["height"], out var height))
            config.Height = height;
        
        if (Request.Query.ContainsKey("orientation"))
            config.Orientation = Request.Query["orientation"].ToString().ToLower();

        // Colores
        if (Request.Query.ContainsKey("primaryColor"))
            config.PrimaryColor = Request.Query["primaryColor"].ToString();
        
        if (Request.Query.ContainsKey("secondaryColor"))
            config.SecondaryColor = Request.Query["secondaryColor"].ToString();
        
        if (Request.Query.ContainsKey("backgroundColor"))
            config.BackgroundColor = Request.Query["backgroundColor"].ToString();
        
        if (Request.Query.ContainsKey("textColor"))
            config.TextColor = Request.Query["textColor"].ToString();

        // Tamaños de elementos
        if (Request.Query.ContainsKey("qrSize") && double.TryParse(Request.Query["qrSize"], out var qrSize))
            config.QrSize = qrSize;
        
        if (Request.Query.ContainsKey("photoWidth") && double.TryParse(Request.Query["photoWidth"], out var photoWidth))
            config.PhotoWidth = photoWidth;
        
        if (Request.Query.ContainsKey("photoHeight") && double.TryParse(Request.Query["photoHeight"], out var photoHeight))
            config.PhotoHeight = photoHeight;

        // Posicionamiento
        if (Request.Query.ContainsKey("photoPosition"))
            config.PhotoPosition = Request.Query["photoPosition"].ToString().ToLower();
        
        if (Request.Query.ContainsKey("qrPosition"))
            config.QrPosition = Request.Query["qrPosition"].ToString().ToLower();
        
        if (Request.Query.ContainsKey("logoPosition"))
            config.LogoPosition = Request.Query["logoPosition"].ToString().ToLower();
        
        if (Request.Query.ContainsKey("textAlignment"))
            config.TextAlignment = Request.Query["textAlignment"].ToString().ToLower();

        // Layout
        if (Request.Query.ContainsKey("layoutStyle"))
            config.LayoutStyle = Request.Query["layoutStyle"].ToString().ToLower();

        // Elementos visibles (sobrescribir valores del template)
        if (Request.Query.ContainsKey("showLogo") && bool.TryParse(Request.Query["showLogo"], out var showLogo))
            config.ShowLogo = showLogo;
        
        if (Request.Query.ContainsKey("showInstitutionName") && bool.TryParse(Request.Query["showInstitutionName"], out var showInstitutionName))
            config.ShowInstitutionName = showInstitutionName;
        
        if (Request.Query.ContainsKey("showUserName") && bool.TryParse(Request.Query["showUserName"], out var showUserName))
            config.ShowUserName = showUserName;
        
        if (Request.Query.ContainsKey("showCardNumber") && bool.TryParse(Request.Query["showCardNumber"], out var showCardNumber))
            config.ShowCardNumber = showCardNumber;
        
        if (Request.Query.ContainsKey("showQrCode") && bool.TryParse(Request.Query["showQrCode"], out var showQrCode))
            config.ShowQrCode = showQrCode;
        
        if (Request.Query.ContainsKey("showPhoto") && bool.TryParse(Request.Query["showPhoto"], out var showPhoto))
            config.ShowPhoto = showPhoto;
        
        if (Request.Query.ContainsKey("showIdentificationNumber") && bool.TryParse(Request.Query["showIdentificationNumber"], out var showId))
            config.ShowIdentificationNumber = showId;
        
        if (Request.Query.ContainsKey("showEmail") && bool.TryParse(Request.Query["showEmail"], out var showEmail))
            config.ShowEmail = showEmail;
        
        if (Request.Query.ContainsKey("showPhone") && bool.TryParse(Request.Query["showPhone"], out var showPhone))
            config.ShowPhone = showPhone;
        
        if (Request.Query.ContainsKey("showDateOfBirth") && bool.TryParse(Request.Query["showDateOfBirth"], out var showDob))
            config.ShowDateOfBirth = showDob;

        // Configuración de dos caras
        if (Request.Query.ContainsKey("doubleSided") && bool.TryParse(Request.Query["doubleSided"], out var doubleSided))
        {
            config.DoubleSided = doubleSided;
        }
        
        if (Request.Query.ContainsKey("qrOnBack") && bool.TryParse(Request.Query["qrOnBack"], out var qrOnBack))
        {
            config.QrOnBack = qrOnBack;
            // Si QR va en la trasera, automáticamente activar dos caras
            if (qrOnBack)
            {
                config.DoubleSided = true;
                config.ShowQrCode = false; // No mostrar QR en el frente
            }
        }
        
        if (Request.Query.ContainsKey("backRotate180") && bool.TryParse(Request.Query["backRotate180"], out var backRotate180))
            config.BackRotate180 = backRotate180;
    }
}

