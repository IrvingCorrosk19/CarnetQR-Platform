using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
public class InstitutionsController : Controller
{
    private readonly IInstitutionService _institutionService;
    private readonly ILogger<InstitutionsController> _logger;

    public InstitutionsController(IInstitutionService institutionService, ILogger<InstitutionsController> logger)
    {
        _institutionService = institutionService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var institutions = await _institutionService.GetAllAsync();
        return View(institutions);
    }

    public IActionResult Create()
    {
        return View(new Institution());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Institution institution)
    {
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
            await _institutionService.CreateAsync(institution);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Empresa creada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Empresa creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating institution");
            var errorMsg = "Error al crear la empresa.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(institution);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var institution = await _institutionService.GetByIdAsync(id);
        if (institution == null)
        {
            return NotFound();
        }

        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Institution institution)
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
            await _institutionService.UpdateAsync(institution);
            
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
}

