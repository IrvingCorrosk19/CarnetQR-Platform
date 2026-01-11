using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Application.Common;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class CardsController : Controller
{
    private readonly ICardService _cardService;
    private readonly IEntityProfileService _entityProfileService;
    private readonly IInstitutionService _institutionService;
    private readonly IAuditService _auditService;
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly ILogger<CardsController> _logger;
    private readonly QrCodeService _qrCodeService;

    public CardsController(
        ICardService cardService,
        IEntityProfileService entityProfileService,
        IInstitutionService institutionService,
        IAuditService auditService,
        UserManager<Domain.Entities.AppUser> userManager,
        ILogger<CardsController> logger,
        QrCodeService qrCodeService)
    {
        _cardService = cardService;
        _entityProfileService = entityProfileService;
        _institutionService = institutionService;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
        _qrCodeService = qrCodeService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var parameters = new PaginationParameters(page, pageSize);
        var pagedCards = await _cardService.GetAllPagedAsync(parameters);
        
        // Pasar datos de paginación a la vista
        ViewBag.CurrentPage = pagedCards.PageNumber;
        ViewBag.PageSize = pagedCards.PageSize;
        ViewBag.TotalPages = pagedCards.TotalPages;
        ViewBag.HasPreviousPage = pagedCards.HasPreviousPage;
        ViewBag.HasNextPage = pagedCards.HasNextPage;
        ViewBag.TotalCount = pagedCards.TotalCount;
        
        return View(pagedCards.Items);
    }

    public async Task<IActionResult> Create()
    {
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        
        // Obtener entidades disponibles (sin carnet activo)
        // Los servicios ya filtran por tenant automáticamente, pero para SuperAdmin necesitamos todas
        IEnumerable<Domain.Entities.EntityProfile> entities;
        IEnumerable<Domain.Entities.Card> cards;
        
        if (isSuperAdmin)
        {
            // SuperAdmin puede ver todas las entidades de todas las instituciones
            // Necesitamos acceder directamente al contexto para evitar el filtro de tenant
            // Pero mejor usamos el servicio que ya maneja esto correctamente
            entities = await _entityProfileService.GetAllAsync();
            cards = await _cardService.GetAllAsync();
            
            // Cargar instituciones para el filtro (opcional)
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        }
        else
        {
            // Para otros roles, los servicios ya filtran por tenant automáticamente
            entities = await _entityProfileService.GetAllAsync();
            cards = await _cardService.GetAllAsync();
        }
        
        // Obtener IDs de entidades que ya tienen carnet activo
        var entitiesWithCards = cards.Where(c => c.IsActive).Select(c => c.EntityProfileId).ToHashSet();
        
        // Filtrar entidades que no tienen carnet activo y están activas
        var availableEntities = entities
            .Where(e => e.IsActive && !entitiesWithCards.Contains(e.Id))
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToList();
        
        ViewBag.AvailableEntities = availableEntities;
        ViewBag.IsSuperAdmin = isSuperAdmin;
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid entityProfileId)
    {
        try
        {
            if (entityProfileId == Guid.Empty)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Debe seleccionar una entidad." });
                }
                ModelState.AddModelError("", "Debe seleccionar una entidad.");
                var entities = await _entityProfileService.GetAllAsync();
                var cards = await _cardService.GetAllAsync();
                var entitiesWithCards = cards.Where(c => c.IsActive).Select(c => c.EntityProfileId).ToHashSet();
                var availableEntities = entities
                    .Where(e => e.IsActive && !entitiesWithCards.Contains(e.Id))
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToList();
                ViewBag.AvailableEntities = availableEntities;
                return View();
            }

            // Verificar si la entidad ya tiene un carnet activo
            var existingCards = await _cardService.GetAllAsync();
            var hasActiveCard = existingCards.Any(c => c.EntityProfileId == entityProfileId && c.IsActive);
            
            if (hasActiveCard)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Esta entidad ya tiene un carnet activo." });
                }
                ModelState.AddModelError("", "Esta entidad ya tiene un carnet activo.");
                var entities = await _entityProfileService.GetAllAsync();
                var cards = await _cardService.GetAllAsync();
                var entitiesWithCards = cards.Where(c => c.IsActive).Select(c => c.EntityProfileId).ToHashSet();
                var availableEntities = entities
                    .Where(e => e.IsActive && !entitiesWithCards.Contains(e.Id))
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToList();
                ViewBag.AvailableEntities = availableEntities;
                return View();
            }

            var card = await _cardService.CreateAsync(entityProfileId);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                card.InstitutionId,
                userId,
                "CREATE",
                "Card",
                card.Id.ToString(),
                new Dictionary<string, object> { { "CardNumber", card.CardNumber }, { "EntityProfileId", entityProfileId.ToString() } });

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"Carnet {card.CardNumber} creado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }

            TempData["SuccessMessage"] = $"Carnet {card.CardNumber} creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error creating card - ArgumentException");
            var errorMsg = ex.Message;
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            var entities = await _entityProfileService.GetAllAsync();
            var cards = await _cardService.GetAllAsync();
            var entitiesWithCards = cards.Where(c => c.IsActive).Select(c => c.EntityProfileId).ToHashSet();
            var availableEntities = entities
                .Where(e => e.IsActive && !entitiesWithCards.Contains(e.Id))
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToList();
            ViewBag.AvailableEntities = availableEntities;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card");
            var errorMsg = $"Error al crear el carnet: {ex.Message}";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            var entities = await _entityProfileService.GetAllAsync();
            var cards = await _cardService.GetAllAsync();
            var entitiesWithCards = cards.Where(c => c.IsActive).Select(c => c.EntityProfileId).ToHashSet();
            var availableEntities = entities
                .Where(e => e.IsActive && !entitiesWithCards.Contains(e.Id))
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToList();
            ViewBag.AvailableEntities = availableEntities;
            return View();
        }
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
            var card = await _cardService.GetByIdAsync(id);
            if (card == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Carnet no encontrado." });
                }
                return NotFound();
            }

            var result = await _cardService.ToggleActiveAsync(id);
            if (!result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo cambiar el estado del carnet." });
                }
                TempData["ErrorMessage"] = "No se pudo cambiar el estado del carnet.";
                return RedirectToAction(nameof(Index));
            }

            // Obtener el carnet actualizado
            var updatedCard = await _cardService.GetByIdAsync(id);
            if (updatedCard != null)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    updatedCard.InstitutionId,
                    userId,
                    "TOGGLE_ACTIVE",
                    "Card",
                    updatedCard.Id.ToString(),
                    new Dictionary<string, object> { { "CardNumber", updatedCard.CardNumber }, { "IsActive", updatedCard.IsActive } });
            }
            
            var message = updatedCard?.IsActive == true ? "Carnet activado exitosamente." : "Carnet desactivado exitosamente.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = message, isActive = updatedCard?.IsActive });
            }

            TempData["SuccessMessage"] = message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado del carnet {CardId}", id);
            var errorMsg = $"Error al cambiar el estado del carnet: {ex.Message}";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var card = await _cardService.GetByIdAsync(id);
            if (card == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Carnet no encontrado." });
                }
                return NotFound();
            }

            var institutionId = card.InstitutionId;
            var cardNumber = card.CardNumber;
            var deleted = await _cardService.DeleteAsync(id);

            if (deleted)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    institutionId,
                    userId,
                    "DELETE",
                    "Card",
                    id.ToString(),
                    new Dictionary<string, object> { { "CardNumber", cardNumber } });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Carnet eliminado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Carnet eliminado exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar el carnet." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar el carnet.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card");
            var errorMsg = $"Error al eliminar el carnet: {ex.Message}";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}

