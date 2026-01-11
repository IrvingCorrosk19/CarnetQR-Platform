using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class InstitutionTypesController : ControllerBase
{
    private readonly IInstitutionTypeService _institutionTypeService;
    private readonly ILogger<InstitutionTypesController> _logger;

    public InstitutionTypesController(
        IInstitutionTypeService institutionTypeService,
        ILogger<InstitutionTypesController> logger)
    {
        _institutionTypeService = institutionTypeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _institutionTypeService.GetAllAsync();
        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var type = await _institutionTypeService.GetByIdAsync(id);
        if (type == null)
        {
            return NotFound(new { success = false, message = "Tipo de institución no encontrado." });
        }
        return Ok(type);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InstitutionType institutionType)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            var created = await _institutionTypeService.CreateAsync(institutionType);
            return Ok(new { success = true, message = "Tipo de institución creado exitosamente.", data = created });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating institution type");
            return StatusCode(500, new { success = false, message = "Error al crear el tipo de institución." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InstitutionType institutionType)
    {
        if (id != institutionType.Id)
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
            var updated = await _institutionTypeService.UpdateAsync(institutionType);
            return Ok(new { success = true, message = "Tipo de institución actualizado exitosamente.", data = updated });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating institution type");
            return StatusCode(500, new { success = false, message = "Error al actualizar el tipo de institución." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _institutionTypeService.DeleteAsync(id);
            if (deleted)
            {
                return Ok(new { success = true, message = "Tipo de institución eliminado exitosamente." });
            }
            return NotFound(new { success = false, message = "Tipo de institución no encontrado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting institution type");
            return StatusCode(500, new { success = false, message = "Error al eliminar el tipo de institución." });
        }
    }

    [HttpPost("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var result = await _institutionTypeService.ToggleActiveAsync(id);
            if (result)
            {
                var type = await _institutionTypeService.GetByIdAsync(id);
                return Ok(new { success = true, message = "Estado actualizado exitosamente.", data = type });
            }
            return NotFound(new { success = false, message = "Tipo de institución no encontrado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling institution type active status");
            return StatusCode(500, new { success = false, message = "Error al actualizar el estado." });
        }
    }
}

