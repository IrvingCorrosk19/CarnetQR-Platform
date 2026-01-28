using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SpecialtiesApiController : ControllerBase
{
    private readonly ISpecialtyService _specialtyService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SpecialtiesApiController> _logger;

    public SpecialtiesApiController(
        ISpecialtyService specialtyService,
        ITenantProvider tenantProvider,
        ILogger<SpecialtiesApiController> logger)
    {
        _specialtyService = specialtyService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var specialties = await _specialtyService.GetAllAsync();
        return Ok(specialties);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var specialty = await _specialtyService.GetByIdAsync(id);
        if (specialty == null)
        {
            return NotFound(new { success = false, message = "Especialidad no encontrada." });
        }
        return Ok(specialty);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Specialty specialty)
    {
        _logger.LogInformation("[SpecialtiesApi/Create] Iniciando creación vía API. Usuario: {User}, Nombre: {Name}, InstitutionId: {InstitutionId}, IsActive: {IsActive}",
            User.Identity?.Name, specialty.Name, specialty.InstitutionId, specialty.IsActive);

        // Remover propiedades de navegación del ModelState
        ModelState.Remove(nameof(specialty.Institution));
        ModelState.Remove(nameof(specialty.Doctors));
        ModelState.Remove(nameof(specialty.Id));
        ModelState.Remove(nameof(specialty.CreatedAt));
        ModelState.Remove(nameof(specialty.UpdatedAt));

        // Validar InstitutionId basado en el rol del usuario
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        if (isSuperAdmin)
        {
            if (specialty.InstitutionId == Guid.Empty)
            {
                _logger.LogWarning("[SpecialtiesApi/Create] SuperAdmin no proporcionó InstitutionId");
                ModelState.AddModelError(nameof(specialty.InstitutionId), "Debe especificar una institución.");
            }
        }
        else
        {
            // Para usuarios no-SuperAdmin, obtener InstitutionId del tenant
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                specialty.InstitutionId = tenantId.Value;
                _logger.LogInformation("[SpecialtiesApi/Create] InstitutionId asignado desde tenant: {InstitutionId}", specialty.InstitutionId);
            }
            else
            {
                _logger.LogWarning("[SpecialtiesApi/Create] No se pudo obtener InstitutionId del tenant");
                ModelState.AddModelError(nameof(specialty.InstitutionId), "No se pudo determinar la institución.");
            }
        }

        if (!ModelState.IsValid)
        {
            var allErrors = new List<string>();
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        var errorMsg = $"{key}: {error.ErrorMessage}";
                        if (string.IsNullOrEmpty(error.ErrorMessage) && error.Exception != null)
                        {
                            errorMsg = $"{key}: {error.Exception.Message}";
                        }
                        allErrors.Add(errorMsg);
                        _logger.LogWarning("[SpecialtiesApi/Create] Error en {Key}: {Error}", key, errorMsg);
                    }
                }
            }
            _logger.LogWarning("[SpecialtiesApi/Create] ModelState inválido. Total errores: {Count}, Errores: {Errors}", 
                allErrors.Count, string.Join(" | ", allErrors));
            return BadRequest(new { success = false, message = string.Join(" ", allErrors) });
        }

        try
        {
            _logger.LogInformation("[SpecialtiesApi/Create] Llamando a SpecialtyService.CreateAsync. Nombre: {Name}", specialty.Name);
            var created = await _specialtyService.CreateAsync(specialty);
            _logger.LogInformation("[SpecialtiesApi/Create] Especialidad creada exitosamente. ID: {Id}, Nombre: {Name}", 
                created.Id, created.Name);
            return Ok(new { success = true, message = "Especialidad creada exitosamente.", data = created });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[SpecialtiesApi/Create] Error de negocio. Nombre: {Name}, Mensaje: {Message}", 
                specialty.Name, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SpecialtiesApi/Create] Error inesperado al crear especialidad. Nombre: {Name}", specialty.Name);
            return StatusCode(500, new { success = false, message = "Error al crear la especialidad." });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Specialty specialty)
    {
        if (id != specialty.Id)
        {
            return BadRequest(new { success = false, message = "ID no coincide." });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            var updated = await _specialtyService.UpdateAsync(specialty);
            return Ok(new { success = true, message = "Especialidad actualizada exitosamente.", data = updated });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating specialty");
            return StatusCode(500, new { success = false, message = "Error al actualizar la especialidad." });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _specialtyService.DeleteAsync(id);
            if (deleted)
            {
                return Ok(new { success = true, message = "Especialidad eliminada exitosamente." });
            }
            return NotFound(new { success = false, message = "Especialidad no encontrada." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting specialty");
            return StatusCode(500, new { success = false, message = "Error al eliminar la especialidad." });
        }
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var result = await _specialtyService.ToggleActiveAsync(id);
            if (result)
            {
                var specialty = await _specialtyService.GetByIdAsync(id);
                return Ok(new { success = true, message = "Estado actualizado exitosamente.", data = specialty });
            }
            return NotFound(new { success = false, message = "Especialidad no encontrada." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling specialty active status");
            return StatusCode(500, new { success = false, message = "Error al actualizar el estado." });
        }
    }
}
