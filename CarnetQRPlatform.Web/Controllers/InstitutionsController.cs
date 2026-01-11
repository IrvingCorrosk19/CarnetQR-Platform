using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
public class InstitutionsController : Controller
{
    private readonly IInstitutionService _institutionService;
    private readonly IInstitutionTypeService _institutionTypeService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<InstitutionsController> _logger;

    public InstitutionsController(
        IInstitutionService institutionService,
        IInstitutionTypeService institutionTypeService,
        UserManager<AppUser> userManager,
        IAuditService auditService,
        ILogger<InstitutionsController> logger)
    {
        _institutionService = institutionService;
        _institutionTypeService = institutionTypeService;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var institutions = await _institutionService.GetAllAsync();
        return View(institutions);
    }

    public async Task<IActionResult> Create()
    {
        var institutionTypes = await _institutionTypeService.GetAllAsync();
        ViewBag.InstitutionTypes = institutionTypes.Where(it => it.IsActive).OrderBy(it => it.Name).ToList();
        return View(new Models.CreateInstitutionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Models.CreateInstitutionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(model);
        }

        try
        {
            // Validar que el tipo de institución existe
            if (model.InstitutionTypeId.HasValue)
            {
                var institutionType = await _institutionTypeService.GetByIdAsync(model.InstitutionTypeId.Value);
                if (institutionType == null || !institutionType.IsActive)
                {
                    ModelState.AddModelError(nameof(model.InstitutionTypeId), "El tipo de institución seleccionado no existe o está inactivo.");
                    var institutionTypes = await _institutionTypeService.GetAllAsync();
                    ViewBag.InstitutionTypes = institutionTypes.Where(it => it.IsActive).OrderBy(it => it.Name).ToList();
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "El tipo de institución seleccionado no existe o está inactivo." });
                    }
                    return View(model);
                }
            }

            // Crear la institución
            var institution = new Institution
            {
                Name = model.Name,
                Description = model.Description,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                CardPrefix = model.CardPrefix,
                InstitutionTypeId = model.InstitutionTypeId,
                IsActive = true
            };

            var createdInstitution = await _institutionService.CreateAsync(institution);

            // Crear el usuario Administrador de la institución
            var adminUser = new AppUser
            {
                UserName = model.AdminEmail,
                Email = model.AdminEmail,
                FirstName = model.AdminFirstName,
                LastName = model.AdminLastName,
                InstitutionId = createdInstitution.Id,
                IsActive = true,
                EmailConfirmed = true
            };

            var createUserResult = await _userManager.CreateAsync(adminUser, model.AdminPassword);
            
            if (createUserResult.Succeeded)
            {
                _logger.LogInformation("User created successfully: {Email}, UserName: {UserName}", 
                    adminUser.Email, adminUser.UserName);
                
                var roleResult = await _userManager.AddToRoleAsync(adminUser, Roles.InstitutionAdmin);
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Role InstitutionAdmin assigned to user: {Email}", model.AdminEmail);
                }
                else
                {
                    _logger.LogError("Error assigning role to user {Email}: {Errors}", 
                        model.AdminEmail, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
                
                // Agregar claim de InstitutionId
                var claimResult = await _userManager.AddClaimAsync(adminUser, new Claim("InstitutionId", createdInstitution.Id.ToString()));
                if (claimResult.Succeeded)
                {
                    _logger.LogInformation("InstitutionId claim added to user: {Email}, InstitutionId: {InstitutionId}", 
                        model.AdminEmail, createdInstitution.Id);
                }
                else
                {
                    _logger.LogError("Error adding claim to user {Email}: {Errors}", 
                        model.AdminEmail, string.Join(", ", claimResult.Errors.Select(e => e.Description)));
                }
                
                _logger.LogInformation("InstitutionAdmin created successfully for institution {InstitutionName}: {Email}", 
                    createdInstitution.Name, model.AdminEmail);
            }
            else
            {
                var errors = string.Join("; ", createUserResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                _logger.LogError("Error creating InstitutionAdmin for {Email}: {Errors}", 
                    model.AdminEmail, errors);
                
                // Si es petición AJAX, retornar error detallado
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = false, 
                        message = $"Error al crear el usuario administrador: {errors}" 
                    });
                }
                
                // Agregar errores al ModelState para mostrarlos en la vista
                foreach (var error in createUserResult.Errors)
                {
                    ModelState.AddModelError("AdminPassword", $"{error.Code}: {error.Description}");
                }
                
                // Continuar aunque falle la creación del admin (se puede crear manualmente después)
                // Pero informar al usuario
                TempData["WarningMessage"] = $"La institución se creó, pero hubo un problema al crear el usuario administrador: {errors}";
            }

            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                createdInstitution.Id,
                userId,
                "CREATE",
                "Institution",
                createdInstitution.Id.ToString(),
                new Dictionary<string, object> { { "Name", createdInstitution.Name } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Empresa y administrador creados exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Empresa y administrador creados exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("prefijo de carnet") || ex.Message.Contains("CardPrefix"))
        {
            _logger.LogWarning(ex, "CardPrefix duplicate error: {Message}", ex.Message);
            var errorMsg = ex.Message;
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError(nameof(model.CardPrefix), errorMsg);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating institution");
            var errorMsg = "Error al crear la empresa.";
            
            // Si el error es de CardPrefix duplicado, mostrar mensaje más específico
            if (ex.InnerException is Npgsql.PostgresException pgEx && 
                pgEx.SqlState == "23505" && 
                pgEx.ConstraintName == "IX_Institutions_CardPrefix")
            {
                errorMsg = $"El prefijo de carnet '{model.CardPrefix}' ya está en uso. Por favor, elija otro prefijo.";
                ModelState.AddModelError(nameof(model.CardPrefix), errorMsg);
            }
            else
            {
                ModelState.AddModelError("", errorMsg);
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var institution = await _institutionService.GetByIdAsync(id);
        if (institution == null)
        {
            return NotFound();
        }

        var institutionTypes = await _institutionTypeService.GetAllAsync();
        ViewBag.InstitutionTypes = institutionTypes.Where(it => it.IsActive).OrderBy(it => it.Name).ToList();
        
        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Institution institution, IFormFile? logoFile)
    {
        if (id != institution.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "ID no coincide." });
            }
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(institution);
        }

        try
        {
            // Manejar upload de logo
            if (logoFile != null && logoFile.Length > 0)
            {
                // Validar tipo de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
                var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Solo se permiten archivos de imagen (JPG, PNG, GIF, SVG).");
                    return View(institution);
                }

                // Validar tamaño (máximo 5MB)
                if (logoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "El archivo no puede ser mayor a 5MB.");
                    return View(institution);
                }

                // Crear directorio si no existe
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generar nombre único
                var fileName = $"{institution.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                var relativePath = $"/uploads/logos/{fileName}";

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                // Eliminar logo anterior si existe
                if (!string.IsNullOrEmpty(institution.LogoPath) && institution.LogoPath.StartsWith("/uploads/logos/"))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", institution.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                institution.LogoPath = relativePath;
            }

            await _institutionService.UpdateAsync(institution);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                institution.Id,
                userId,
                "UPDATE",
                "Institution",
                institution.Id.ToString(),
                new Dictionary<string, object> { { "Name", institution.Name } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Empresa actualizada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Empresa actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating institution");
            var errorMsg = "Error al actualizar la empresa.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(institution);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _institutionService.ToggleActiveAsync(id);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result)
            {
                return Json(new { success = true, message = "Estado de la empresa actualizado." });
            }
            else
            {
                return Json(new { success = false, message = "Error al actualizar el estado." });
            }
        }
        
        if (result)
        {
            TempData["SuccessMessage"] = "Estado de la empresa actualizado.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al actualizar el estado.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetInstitutionPhotoEnabled(Guid id)
    {
        var institution = await _institutionService.GetByIdAsync(id);
        if (institution == null)
        {
            return Json(new { photoEnabled = false });
        }
        return Json(new { photoEnabled = institution.PhotoEnabled });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var institution = await _institutionService.GetByIdAsync(id);
            if (institution == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Institución no encontrada." });
                }
                return NotFound();
            }

            var institutionName = institution.Name;
            var deleted = await _institutionService.DeleteAsync(id);

            if (deleted)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    id,
                    userId,
                    "DELETE",
                    "Institution",
                    id.ToString(),
                    new Dictionary<string, object> { { "Name", institutionName } });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Institución eliminada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Institución eliminada exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar la institución." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar la institución.";
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation deleting institution");
            var errorMsg = ex.Message;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting institution");
            var errorMsg = $"Error al eliminar la institución: {ex.Message}";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}

