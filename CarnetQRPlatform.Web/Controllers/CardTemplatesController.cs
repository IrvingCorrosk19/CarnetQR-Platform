using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "InstitutionAdminOrAbove")]
public class CardTemplatesController : Controller
{
    private readonly ICardTemplateService _templateService;
    private readonly IInstitutionService _institutionService;
    private readonly ILogger<CardTemplatesController> _logger;
    private readonly IWebHostEnvironment _environment;

    public CardTemplatesController(
        ICardTemplateService templateService,
        IInstitutionService institutionService,
        ILogger<CardTemplatesController> logger,
        IWebHostEnvironment environment)
    {
        _templateService = templateService;
        _institutionService = institutionService;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _templateService.GetAllAsync();
        return View(templates);
    }

    public IActionResult Create()
    {
        var template = new CardTemplate
        {
            PhotoEnabled = true,
            VisibleFields = new List<string> { "IdentificationNumber", "FirstName", "LastName" }
        };
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardTemplate template)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(template);
        }

        try
        {
            // Validar máximo 6 campos visibles
            if (template.VisibleFields != null && template.VisibleFields.Count > 6)
            {
                var errorMsg = "Máximo 6 campos visibles permitidos.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMsg });
                }
                ModelState.AddModelError("VisibleFields", errorMsg);
                return View(template);
            }

            await _templateService.CreateAsync(template);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Plantilla creada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Plantilla creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card template");
            var errorMsg = "Error al crear la plantilla.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(template);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var template = await _templateService.GetByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // Cargar la institución para mostrar el logo
        if (template.InstitutionId != Guid.Empty)
        {
            var institution = await _institutionService.GetByIdAsync(template.InstitutionId);
            if (institution != null)
            {
                template.Institution = institution;
            }
        }

        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CardTemplate template)
    {
        if (id != template.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(template);
        }

        try
        {
            // Validar máximo 6 campos visibles
            if (template.VisibleFields != null && template.VisibleFields.Count > 6)
            {
                var errorMsg = "Máximo 6 campos visibles permitidos.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMsg });
                }
                ModelState.AddModelError("VisibleFields", errorMsg);
                return View(template);
            }

            await _templateService.UpdateAsync(template);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Plantilla actualizada exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Plantilla actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card template");
            var errorMsg = "Error al actualizar la plantilla.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(template);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _templateService.DeleteAsync(id);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Plantilla eliminada exitosamente." });
                }
                TempData["SuccessMessage"] = "Plantilla eliminada exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar la plantilla." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar la plantilla.";
            }
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card template");
            var errorMsg = "Error al eliminar la plantilla.";
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
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        try
        {
            var result = await _templateService.SetAsDefaultAsync(id);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Plantilla marcada como predeterminada." });
                }
                TempData["SuccessMessage"] = "Plantilla marcada como predeterminada.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo marcar la plantilla como predeterminada." });
                }
                TempData["ErrorMessage"] = "No se pudo marcar la plantilla como predeterminada.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default template");
            var errorMsg = "Error al establecer la plantilla predeterminada.";
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
    public async Task<IActionResult> UploadLogo(IFormFile logoFile, Guid templateId)
    {
        if (logoFile == null || logoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No se seleccionó ningún archivo.";
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }

        // Validar tipo de archivo por extensión
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
        var fileExtension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            TempData["ErrorMessage"] = "Formato de archivo no permitido. Use JPG, PNG, GIF o SVG.";
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }

        // Validar tamaño (máximo 5MB)
        if (logoFile.Length > 5 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "El archivo es demasiado grande. Máximo 5MB.";
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }

        // Validar MIME type real del archivo (seguridad crítica)
        var allowedMimeTypes = new[] { 
            "image/jpeg", 
            "image/jpg", 
            "image/png", 
            "image/gif", 
            "image/svg+xml" 
        };
        
        if (string.IsNullOrEmpty(logoFile.ContentType) || !allowedMimeTypes.Contains(logoFile.ContentType.ToLowerInvariant()))
        {
            TempData["ErrorMessage"] = "Tipo de archivo no válido. Solo se permiten imágenes.";
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }

        // Validar firma del archivo (magic bytes) para prevenir uploads maliciosos
        var isValidImage = await ValidateImageFileSignature(logoFile);
        if (!isValidImage)
        {
            TempData["ErrorMessage"] = "El archivo no es una imagen válida.";
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }

        try
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var relativePath = $"/uploads/logos/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            var template = await _templateService.GetByIdAsync(templateId);
            if (template != null && template.InstitutionId != Guid.Empty)
            {
                // Guardar el logo en la institución (el logo es por institución, no por plantilla)
                var institution = await _institutionService.GetByIdAsync(template.InstitutionId);
                if (institution != null)
                {
                    institution.LogoPath = relativePath;
                    await _institutionService.UpdateAsync(institution);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Logo subido exitosamente.", logoPath = relativePath });
            }
            
            TempData["SuccessMessage"] = "Logo subido exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo");
            var errorMsg = "Error al subir el logo.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Edit), new { id = templateId });
    }

    private async Task<bool> ValidateImageFileSignature(IFormFile file)
    {
        // Leer los primeros bytes para validar la firma del archivo
        using var stream = file.OpenReadStream();
        var buffer = new byte[12];
        await stream.ReadAsync(buffer, 0, buffer.Length);
        stream.Position = 0; // Reset para que el archivo pueda ser guardado después

        // Validar magic bytes según el tipo de archivo
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        return extension switch
        {
            ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            ".gif" => buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38,
            ".svg" => System.Text.Encoding.UTF8.GetString(buffer).Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
                      System.Text.Encoding.UTF8.GetString(buffer).Contains("<?xml", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

