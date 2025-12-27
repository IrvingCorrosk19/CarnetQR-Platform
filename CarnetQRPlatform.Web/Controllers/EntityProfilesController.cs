using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class EntityProfilesController : Controller
{
    private readonly IEntityProfileService _entityProfileService;
    private readonly ICardService _cardService;
    private readonly IInstitutionService _institutionService;
    private readonly ILogger<EntityProfilesController> _logger;

    public EntityProfilesController(
        IEntityProfileService entityProfileService,
        ICardService cardService,
        IInstitutionService institutionService,
        ILogger<EntityProfilesController> logger)
    {
        _entityProfileService = entityProfileService;
        _cardService = cardService;
        _institutionService = institutionService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var entities = await _entityProfileService.GetAllAsync();
        return View(entities);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _entityProfileService.GetByIdAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        var cards = await _cardService.GetAllAsync();
        ViewBag.Cards = cards.Where(c => c.EntityProfileId == id);

        return View(entity);
    }

    public async Task<IActionResult> Create()
    {
        var model = new EntityProfile();
        
        // Si es SuperAdmin, cargar lista de instituciones para seleccionar
        if (User.IsInRole(Roles.SuperAdmin))
        {
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EntityProfile entityProfile)
    {
        try
        {
            // Si es SuperAdmin, validar que haya seleccionado una institución
            if (User.IsInRole(Roles.SuperAdmin))
            {
                if (entityProfile.InstitutionId == Guid.Empty)
                {
                    ModelState.AddModelError(nameof(entityProfile.InstitutionId), "Debe seleccionar una empresa.");
                    
                    // Recargar instituciones para el dropdown
                    var institutions = await _institutionService.GetAllAsync();
                    ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Debe seleccionar una empresa." });
                    }
                    return View(entityProfile);
                }
                
                // Validar que la institución existe y está activa
                var institution = await _institutionService.GetByIdAsync(entityProfile.InstitutionId);
                if (institution == null || !institution.IsActive)
                {
                    ModelState.AddModelError(nameof(entityProfile.InstitutionId), "La empresa seleccionada no existe o está inactiva.");
                    
                    var institutions = await _institutionService.GetAllAsync();
                    ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "La empresa seleccionada no existe o está inactiva." });
                    }
                    return View(entityProfile);
                }
            }
            else
            {
                // InstitutionId se establece automáticamente desde el tenant, remover del modelo para evitar validación
                ModelState.Remove(nameof(entityProfile.InstitutionId));
            }
            
            ModelState.Remove(nameof(entityProfile.Institution)); // Remover también la propiedad de navegación
            
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                var errorMessage = string.Join(" ", errorMessages);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                return View(entityProfile);
            }

            var created = await _entityProfileService.CreateAsync(entityProfile);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Entidad creada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Entidad creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity profile. InstitutionId={InstitutionId}", entityProfile.InstitutionId);
            var errorMsg = "Error al crear la entidad.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(entityProfile);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _entityProfileService.GetByIdAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EntityProfile entityProfile)
    {
        if (id != entityProfile.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "ID no coincide." });
            }
            return NotFound();
        }

        // InstitutionId ya está establecido desde el modelo cargado, no necesita validación
        ModelState.Remove(nameof(entityProfile.InstitutionId));

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(entityProfile);
        }

        try
        {
            await _entityProfileService.UpdateAsync(entityProfile);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Entidad actualizada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Entidad actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity profile");
            var errorMsg = "Error al actualizar la entidad.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(entityProfile);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCard(Guid entityProfileId)
    {
        System.Console.WriteLine("=== [EntityProfilesController] GenerateCard ===");
        System.Console.WriteLine($"[Controller] EntityProfileId: {entityProfileId}");
        
        try
        {
            System.Console.WriteLine("[Controller] Calling CardService.CreateAsync...");
            var card = await _cardService.CreateAsync(entityProfileId);
            System.Console.WriteLine($"[Controller] Card created successfully - Id: {card.Id}, CardNumber: {card.CardNumber}");
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Carnet generado exitosamente.", cardNumber = card.CardNumber });
            }
            TempData["SuccessMessage"] = $"Carnet generado exitosamente. Número: {card.CardNumber}";
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"[Controller] ArgumentException: {ex.Message}");
            _logger.LogError(ex, "Error generating card - ArgumentException");
            var errorMsg = ex.Message;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            TempData["ErrorMessage"] = errorMsg;
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"[Controller] InvalidOperationException: {ex.Message}");
            _logger.LogError(ex, "Error generating card - InvalidOperationException");
            var errorMsg = ex.Message;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            TempData["ErrorMessage"] = errorMsg;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Controller] Exception: {ex.Message}");
            System.Console.WriteLine($"[Controller] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error generating card");
            var errorMsg = $"Error al generar el carnet: {ex.Message}";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            TempData["ErrorMessage"] = errorMsg;
        }
        finally
        {
            System.Console.WriteLine("=== [EntityProfilesController] GenerateCard END ===");
        }

        return RedirectToAction(nameof(Details), new { id = entityProfileId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        System.Console.WriteLine("=== [EntityProfilesController] Delete ===");
        System.Console.WriteLine($"[Controller] Delete called with ID: {id}");
        
        try
        {
            System.Console.WriteLine("[Controller] Getting entity by ID...");
            var entity = await _entityProfileService.GetByIdAsync(id);
            if (entity == null)
            {
                System.Console.WriteLine("[Controller] Entity not found!");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Entidad no encontrada." });
                }
                return NotFound();
            }

            System.Console.WriteLine($"[Controller] Entity found: {entity.FirstName} {entity.LastName}, InstitutionId: {entity.InstitutionId}");
            System.Console.WriteLine("[Controller] Calling DeleteAsync...");
            
            var deleted = await _entityProfileService.DeleteAsync(id);
            
            System.Console.WriteLine($"[Controller] DeleteAsync returned: {deleted}");
            
            if (!deleted)
            {
                System.Console.WriteLine("[Controller] DeleteAsync returned false");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar la entidad." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar la entidad.";
                return RedirectToAction(nameof(Index));
            }

            System.Console.WriteLine("[Controller] Delete successful!");
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Entidad eliminada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }

            TempData["SuccessMessage"] = "Entidad eliminada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"[Controller] InvalidOperationException: {ex.Message}");
            System.Console.WriteLine($"[Controller] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Business rule violation deleting entity profile");
            var errorMsg = ex.Message;
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Controller] Exception: {ex.Message}");
            System.Console.WriteLine($"[Controller] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error deleting entity profile");
            var errorMsg = $"Error al eliminar la entidad: {ex.Message}";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            System.Console.WriteLine("=== [EntityProfilesController] Delete END ===");
        }
    }
}

