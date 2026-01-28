using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "InstitutionAdminOrAbove")]
public class SpecialtiesController : Controller
{
    private readonly ISpecialtyService _specialtyService;
    private readonly IInstitutionService _institutionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SpecialtiesController> _logger;

    public SpecialtiesController(
        ISpecialtyService specialtyService,
        IInstitutionService institutionService,
        ITenantProvider tenantProvider,
        ILogger<SpecialtiesController> logger)
    {
        _specialtyService = specialtyService;
        _institutionService = institutionService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("[Specialties/Index] Usuario: {User}, Roles: {Roles}", 
            User.Identity?.Name, string.Join(", ", User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)));
        
        try
        {
            var specialties = await _specialtyService.GetAllAsync();
            _logger.LogInformation("[Specialties/Index] Se obtuvieron {Count} especialidades", specialties.Count());
            return View(specialties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Specialties/Index] Error al obtener especialidades");
            throw;
        }
    }

    public async Task<IActionResult> Create()
    {
        _logger.LogInformation("[Specialties/Create GET] Usuario: {User}", User.Identity?.Name);
        
        var model = new Specialty();
        
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
    public async Task<IActionResult> Create(Specialty specialty)
    {
        _logger.LogInformation("[Specialties/Create POST] Iniciando creación. Usuario: {User}, Nombre: {Name}, InstitutionId: {InstitutionId}, IsActive: {IsActive}",
            User.Identity?.Name, specialty.Name, specialty.InstitutionId, specialty.IsActive);

        // Remover propiedades de navegación del ModelState
        ModelState.Remove(nameof(specialty.Institution));

        // Si es SuperAdmin, validar que haya seleccionado una institución
        if (User.IsInRole(Roles.SuperAdmin))
        {
            _logger.LogInformation("[Specialties/Create POST] Usuario es SuperAdmin. Validando InstitutionId: {InstitutionId}", specialty.InstitutionId);
            if (specialty.InstitutionId == Guid.Empty)
            {
                _logger.LogWarning("[Specialties/Create POST] SuperAdmin no seleccionó institución");
                ModelState.AddModelError(nameof(specialty.InstitutionId), "Debe seleccionar una institución.");
            }
        }
        else
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogInformation("[Specialties/Create POST] Usuario no-SuperAdmin. TenantId: {TenantId}", tenantId);
        }

        if (!ModelState.IsValid)
        {
            // Recargar datos para la vista
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(specialty);
        }
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("[Specialties/Create POST] ModelState inválido. Errores: {Errors}", string.Join(" | ", errors));
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(specialty);
        }

        try
        {
            _logger.LogInformation("[Specialties/Create POST] Llamando a SpecialtyService.CreateAsync. Nombre: {Name}", specialty.Name);
            var createdSpecialty = await _specialtyService.CreateAsync(specialty);
            _logger.LogInformation("[Specialties/Create POST] Especialidad creada exitosamente. ID: {Id}, Nombre: {Name}", 
                createdSpecialty.Id, createdSpecialty.Name);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Especialidad creada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Especialidad creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[Specialties/Create POST] Error de negocio al crear especialidad. Nombre: {Name}, Mensaje: {Message}", 
                specialty.Name, ex.Message);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("", ex.Message);
            return View(specialty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Specialties/Create POST] Error inesperado al crear especialidad. Nombre: {Name}", specialty.Name);
            var errorMsg = "Error al crear la especialidad.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(specialty);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var specialty = await _specialtyService.GetByIdAsync(id);
        if (specialty == null)
        {
            return NotFound();
        }
        
        return View(specialty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Specialty specialty)
    {
        if (id != specialty.Id)
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
            return View(specialty);
        }

        try
        {
            await _specialtyService.UpdateAsync(specialty);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Especialidad actualizada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Especialidad actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error updating specialty: {Message}", ex.Message);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("", ex.Message);
            return View(specialty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating specialty");
            var errorMsg = "Error al actualizar la especialidad.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(specialty);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _specialtyService.ToggleActiveAsync(id);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result)
            {
                return Json(new { success = true, message = "Estado de la especialidad actualizado." });
            }
            else
            {
                return Json(new { success = false, message = "Error al actualizar el estado." });
            }
        }
        
        if (result)
        {
            TempData["SuccessMessage"] = "Estado de la especialidad actualizado.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al actualizar el estado.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var specialty = await _specialtyService.GetByIdAsync(id);
            if (specialty == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Especialidad no encontrada." });
                }
                return NotFound();
            }

            var specialtyName = specialty.Name;
            var deleted = await _specialtyService.DeleteAsync(id);

            if (deleted)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Especialidad eliminada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Especialidad eliminada exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar la especialidad." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar la especialidad.";
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation deleting specialty");
            var errorMsg = ex.Message;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting specialty");
            var errorMsg = $"Error al eliminar la especialidad: {ex.Message}";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}
