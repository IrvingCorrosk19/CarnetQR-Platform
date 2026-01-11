using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.IO;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class EntityProfilesController : Controller
{
    private readonly IEntityProfileService _entityProfileService;
    private readonly ICardService _cardService;
    private readonly IInstitutionService _institutionService;
    private readonly IAuditService _auditService;
    private readonly ITenantProvider _tenantProvider;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<EntityProfilesController> _logger;

    public EntityProfilesController(
        IEntityProfileService entityProfileService,
        ICardService cardService,
        IInstitutionService institutionService,
        IAuditService auditService,
        ITenantProvider tenantProvider,
        UserManager<AppUser> userManager,
        ILogger<EntityProfilesController> logger)
    {
        _entityProfileService = entityProfileService;
        _cardService = cardService;
        _institutionService = institutionService;
        _auditService = auditService;
        _tenantProvider = tenantProvider;
        _userManager = userManager;
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
            // Para SuperAdmin, PhotoEnabled se determinará dinámicamente según la institución seleccionada
            ViewBag.PhotoEnabled = false; // Se actualizará con JavaScript
        }
        else
        {
            // Obtener institución del tenant para verificar PhotoEnabled
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                var institution = await _institutionService.GetByIdAsync(tenantId.Value);
                ViewBag.PhotoEnabled = institution?.PhotoEnabled ?? false;
            }
            else
            {
                ViewBag.PhotoEnabled = false;
            }
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
                var selectedInstitution = await _institutionService.GetByIdAsync(entityProfile.InstitutionId);
                if (selectedInstitution == null || !selectedInstitution.IsActive)
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
            ModelState.Remove(nameof(entityProfile.PhotoPath)); // PhotoPath se maneja por separado
            
            // Obtener institución para verificar PhotoEnabled
            Institution? institution = null;
            if (User.IsInRole(Roles.SuperAdmin))
            {
                institution = await _institutionService.GetByIdAsync(entityProfile.InstitutionId);
            }
            else
            {
                var tenantId = _tenantProvider.GetCurrentTenantId();
                if (tenantId.HasValue)
                {
                    institution = await _institutionService.GetByIdAsync(tenantId.Value);
                }
            }
            
            // Manejar upload de foto - SIEMPRE permitido (no depende de PhotoEnabled)
            var photoFile = Request.Form.Files["PhotoFile"];
            if (photoFile != null && photoFile.Length > 0)
            {
                // Validar tipo de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("PhotoFile", "Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF).");
                }
                else
                {
                    // Validar tamaño (máximo 5MB)
                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PhotoFile", "El archivo no puede exceder 5MB.");
                    }
                    else
                    {
                        // Validar magic bytes para seguridad adicional
                        var isValidImage = await ValidateImageFile(photoFile);
                        if (!isValidImage)
                        {
                            ModelState.AddModelError("PhotoFile", "El archivo no es una imagen válida.");
                        }
                        else
                        {
                            // Crear directorio si no existe
                            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
                            if (!Directory.Exists(uploadsDir))
                            {
                                Directory.CreateDirectory(uploadsDir);
                            }

                            // Generar nombre único
                            var fileName = $"{Guid.NewGuid()}{extension}";
                            var filePath = Path.Combine(uploadsDir, fileName);
                            var relativePath = $"/uploads/photos/{fileName}";

                            // Guardar archivo
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await photoFile.CopyToAsync(stream);
                            }

                            entityProfile.PhotoPath = relativePath;
                        }
                    }
                }
            }
            
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
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            var institutionId = created.InstitutionId;
            await _auditService.LogActionAsync(
                institutionId,
                userId,
                "CREATE",
                "EntityProfile",
                created.Id.ToString(),
                new Dictionary<string, object> { { "Name", $"{created.FirstName} {created.LastName}" } });
            
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

        // Obtener institución para verificar PhotoEnabled
        var institution = await _institutionService.GetByIdAsync(entity.InstitutionId);
        ViewBag.PhotoEnabled = institution?.PhotoEnabled ?? false;

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

        // Obtener la entidad existente para preservar PhotoPath si no se carga nueva foto
        var existingEntity = await _entityProfileService.GetByIdAsync(id);
        if (existingEntity == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Entidad no encontrada." });
            }
            return NotFound();
        }

        // InstitutionId ya está establecido desde el modelo cargado, no necesita validación
        ModelState.Remove(nameof(entityProfile.InstitutionId));
        ModelState.Remove(nameof(entityProfile.PhotoPath)); // PhotoPath se maneja por separado
        
        // Preservar PhotoPath existente por defecto
        var currentPhotoPath = existingEntity.PhotoPath;
        
        // Manejar upload de foto - SIEMPRE permitido (no depende de PhotoEnabled)
        var photoFile = Request.Form.Files["PhotoFile"];
        if (photoFile != null && photoFile.Length > 0)
        {
            // Validar tipo de archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("PhotoFile", "Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF).");
            }
            else
            {
                // Validar tamaño (máximo 5MB)
                if (photoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("PhotoFile", "El archivo no puede exceder 5MB.");
                }
                else
                {
                    // Validar magic bytes para seguridad adicional
                    var isValidImage = await ValidateImageFile(photoFile);
                    if (!isValidImage)
                    {
                        ModelState.AddModelError("PhotoFile", "El archivo no es una imagen válida.");
                    }
                    else
                    {
                        // Crear directorio si no existe
                        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

                        // Generar nombre único
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsDir, fileName);
                        var relativePath = $"/uploads/photos/{fileName}";

                        // Guardar archivo
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photoFile.CopyToAsync(stream);
                        }

                        // Eliminar foto anterior si existe
                        if (!string.IsNullOrEmpty(currentPhotoPath) && currentPhotoPath.StartsWith("/uploads/photos/"))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", currentPhotoPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "No se pudo eliminar la foto anterior: {PhotoPath}", currentPhotoPath);
                                }
                            }
                        }

                        // Asignar nueva foto
                        entityProfile.PhotoPath = relativePath;
                        currentPhotoPath = relativePath; // Actualizar para asegurar que se use
                    }
                }
            }
        }
        else
        {
            // Si no se carga nueva foto, preservar la foto existente
            entityProfile.PhotoPath = currentPhotoPath;
        }

        // Asegurar que PhotoPath esté establecido (preservar existente o usar nueva)
        if (string.IsNullOrEmpty(entityProfile.PhotoPath))
        {
            entityProfile.PhotoPath = currentPhotoPath;
        }

        if (!ModelState.IsValid)
        {
            // Si hay errores, recargar ViewBag necesario para la vista
            var institution = await _institutionService.GetByIdAsync(existingEntity.InstitutionId);
            ViewBag.PhotoEnabled = institution?.PhotoEnabled ?? false;
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(entityProfile);
        }

        try
        {
            // Asegurar que PhotoPath se preserve si no se carga nueva foto
            if (string.IsNullOrEmpty(entityProfile.PhotoPath))
            {
                entityProfile.PhotoPath = currentPhotoPath;
                _logger.LogInformation("Preservando PhotoPath existente: {PhotoPath} para EntityProfile {Id}", currentPhotoPath ?? "null", entityProfile.Id);
            }
            else
            {
                _logger.LogInformation("Usando nuevo PhotoPath: {PhotoPath} para EntityProfile {Id}", entityProfile.PhotoPath, entityProfile.Id);
            }
            
            // Log final antes de actualizar
            _logger.LogInformation("Actualizando EntityProfile {Id} con PhotoPath: {PhotoPath}", entityProfile.Id, entityProfile.PhotoPath ?? "null");
            
            var updated = await _entityProfileService.UpdateAsync(entityProfile);
            
            _logger.LogInformation("EntityProfile {Id} actualizado exitosamente. PhotoPath guardado: {PhotoPath}", updated.Id, updated.PhotoPath ?? "null");
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                updated.InstitutionId,
                userId,
                "UPDATE",
                "EntityProfile",
                updated.Id.ToString(),
                new Dictionary<string, object> { { "Name", $"{updated.FirstName} {updated.LastName}" }, { "PhotoPath", updated.PhotoPath ?? "null" } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Entidad actualizada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Entidad actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity profile {Id}. PhotoPath intentado: {PhotoPath}", entityProfile.Id, entityProfile.PhotoPath ?? "null");
            var errorMsg = $"Error al actualizar la entidad: {ex.Message}";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg, exception = ex.Message });
            }
            
            ModelState.AddModelError("", errorMsg);
            var institution = await _institutionService.GetByIdAsync(existingEntity.InstitutionId);
            ViewBag.PhotoEnabled = institution?.PhotoEnabled ?? false;
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
            
            var institutionId = entity.InstitutionId;
            var entityName = $"{entity.FirstName} {entity.LastName}";
            
            var deleted = await _entityProfileService.DeleteAsync(id);
            
            System.Console.WriteLine($"[Controller] DeleteAsync returned: {deleted}");
            
            if (deleted)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    institutionId,
                    userId,
                    "DELETE",
                    "EntityProfile",
                    id.ToString(),
                    new Dictionary<string, object> { { "Name", entityName } });
            }
            
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var entity = await _entityProfileService.GetByIdAsync(id);
            if (entity == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Entidad no encontrada." });
                }
                return NotFound();
            }

            var oldStatus = entity.IsActive;
            var result = await _entityProfileService.ToggleActiveAsync(id);

            if (result)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    entity.InstitutionId,
                    userId,
                    "TOGGLE_ACTIVE",
                    "EntityProfile",
                    id.ToString(),
                    new Dictionary<string, object> { { "Name", $"{entity.FirstName} {entity.LastName}" }, { "OldStatus", oldStatus }, { "NewStatus", !oldStatus } });

                var message = !oldStatus ? "Entidad activada exitosamente." : "Entidad desactivada exitosamente.";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = message, isActive = !oldStatus });
                }

                TempData["SuccessMessage"] = message;
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Error al cambiar el estado de la entidad." });
                }
                TempData["ErrorMessage"] = "Error al cambiar el estado de la entidad.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling entity profile active status");
            var errorMsg = "Error al cambiar el estado de la entidad.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateImageFile(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[4];
            await stream.ReadAsync(buffer, 0, 4);
            stream.Position = 0;

            // Verificar magic bytes de imágenes comunes
            // JPEG: FF D8 FF
            // PNG: 89 50 4E 47
            // GIF: 47 49 46 38
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) // JPEG
                return true;
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) // PNG
                return true;
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38) // GIF
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}


