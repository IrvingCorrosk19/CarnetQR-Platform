using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.IO;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize] // Permitir acceso autenticado, validar rol en GetCurrentInstitutionAsync
public class InstitutionConfigController : Controller
{
    private readonly IInstitutionService _institutionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<InstitutionConfigController> _logger;

    public InstitutionConfigController(
        IInstitutionService institutionService,
        ITenantProvider tenantProvider,
        IAuditService auditService,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<InstitutionConfigController> logger)
    {
        _institutionService = institutionService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var institution = await GetCurrentInstitutionAsync();
            if (institution == null)
            {
                _logger.LogWarning("InstitutionConfig/Index: Institution not found. User: {UserId}, IsSuperAdmin: {IsSuperAdmin}", 
                    _userManager.GetUserId(User), _tenantProvider.IsSuperAdmin());
                
                // Si es SuperAdmin, redirigir a AccessDenied con mensaje claro
                if (_tenantProvider.IsSuperAdmin())
                {
                    _logger.LogWarning("SuperAdmin attempted to access InstitutionConfig. Redirecting to AccessDenied. User: {UserId}", 
                        _userManager.GetUserId(User));
                    return RedirectToAction("AccessDenied", "Account", new { returnUrl = Request.Path + Request.QueryString });
                }
                
                // Si no tiene InstitutionId, mostrar mensaje más claro
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !user.InstitutionId.HasValue)
                {
                    TempData["ErrorMessage"] = "Su cuenta de usuario no está asociada a ninguna institución. Contacte al administrador del sistema.";
                    return RedirectToAction("Index", "Home");
                }
                
                TempData["ErrorMessage"] = "No se pudo encontrar la información de su institución. Contacte al administrador del sistema.";
                return RedirectToAction("Index", "Home");
            }

            return View(institution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InstitutionConfig/Index");
            TempData["ErrorMessage"] = "Ocurrió un error al cargar la configuración de la institución.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> CardSettings()
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        // Campos disponibles para el carnet
        ViewBag.AvailableFields = new List<SelectListItem>
        {
            new SelectListItem { Value = "CardNumber", Text = "Número de Carnet" },
            new SelectListItem { Value = "FirstName", Text = "Nombre" },
            new SelectListItem { Value = "LastName", Text = "Apellido" },
            new SelectListItem { Value = "IdentificationNumber", Text = "Número de Identificación" },
            new SelectListItem { Value = "Email", Text = "Correo Electrónico" },
            new SelectListItem { Value = "Phone", Text = "Teléfono" },
            new SelectListItem { Value = "DateOfBirth", Text = "Fecha de Nacimiento" }
        };

        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CardSettings(
        bool photoEnabled,
        List<string> visibleFields,
        QrPublicDisplayMode qrPublicDisplayMode)
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        // Validar máximo 6 campos visibles
        if (visibleFields != null && visibleFields.Count > 6)
        {
            ModelState.AddModelError("", "Máximo 6 campos visibles permitidos.");
            ViewBag.AvailableFields = new List<SelectListItem>
            {
                new SelectListItem { Value = "CardNumber", Text = "Número de Carnet" },
                new SelectListItem { Value = "FirstName", Text = "Nombre" },
                new SelectListItem { Value = "LastName", Text = "Apellido" },
                new SelectListItem { Value = "IdentificationNumber", Text = "Número de Identificación" },
                new SelectListItem { Value = "Email", Text = "Correo Electrónico" },
                new SelectListItem { Value = "Phone", Text = "Teléfono" },
                new SelectListItem { Value = "DateOfBirth", Text = "Fecha de Nacimiento" }
            };
            return View(institution);
        }

        institution.PhotoEnabled = photoEnabled;
        institution.VisibleFields = visibleFields ?? new List<string>();
        institution.QrPublicDisplayMode = qrPublicDisplayMode;

        try
        {
            await _institutionService.UpdateAsync(institution);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Configuración del carnet actualizada exitosamente." });
            }
            
            TempData["SuccessMessage"] = "Configuración del carnet actualizada exitosamente.";
            return RedirectToAction(nameof(CardSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card settings");
            var errorMsg = "Error al actualizar la configuración del carnet.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            ViewBag.AvailableFields = new List<SelectListItem>
            {
                new SelectListItem { Value = "CardNumber", Text = "Número de Carnet" },
                new SelectListItem { Value = "FirstName", Text = "Nombre" },
                new SelectListItem { Value = "LastName", Text = "Apellido" },
                new SelectListItem { Value = "IdentificationNumber", Text = "Número de Identificación" },
                new SelectListItem { Value = "Email", Text = "Correo Electrónico" },
                new SelectListItem { Value = "Phone", Text = "Teléfono" },
                new SelectListItem { Value = "DateOfBirth", Text = "Fecha de Nacimiento" }
            };
            return View(institution);
        }
    }

    [HttpGet]
    public async Task<IActionResult> QrPublicSettings()
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QrPublicSettings(string instructions)
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        institution.Instructions = instructions;

        try
        {
            await _institutionService.UpdateAsync(institution);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Configuración del QR público actualizada exitosamente." });
            }
            
            TempData["SuccessMessage"] = "Configuración del QR público actualizada exitosamente.";
            return RedirectToAction(nameof(QrPublicSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating QR public settings");
            var errorMsg = "Error al actualizar la configuración del QR público.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(institution);
        }
    }

    [HttpGet]
    public async Task<IActionResult> PatientDataVisibility()
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        // Campos disponibles para configurar visibilidad
        ViewBag.AvailableFields = new List<SelectListItem>
        {
            new SelectListItem { Value = "FirstName", Text = "Nombre" },
            new SelectListItem { Value = "LastName", Text = "Apellido" },
            new SelectListItem { Value = "IdentificationNumber", Text = "Número de Identificación" },
            new SelectListItem { Value = "Email", Text = "Correo Electrónico" },
            new SelectListItem { Value = "Phone", Text = "Teléfono" },
            new SelectListItem { Value = "DateOfBirth", Text = "Fecha de Nacimiento" },
            new SelectListItem { Value = "Photo", Text = "Foto" }
        };

        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PatientDataVisibility(Dictionary<string, bool> patientDataVisibilityConfig)
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        institution.PatientDataVisibilityConfig = patientDataVisibilityConfig ?? new Dictionary<string, bool>();

        try
        {
            await _institutionService.UpdateAsync(institution);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Configuración de visibilidad actualizada exitosamente." });
            }
            
            TempData["SuccessMessage"] = "Configuración de visibilidad actualizada exitosamente.";
            return RedirectToAction(nameof(PatientDataVisibility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient data visibility");
            var errorMsg = "Error al actualizar la configuración de visibilidad.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            ViewBag.AvailableFields = new List<SelectListItem>
            {
                new SelectListItem { Value = "FirstName", Text = "Nombre" },
                new SelectListItem { Value = "LastName", Text = "Apellido" },
                new SelectListItem { Value = "IdentificationNumber", Text = "Número de Identificación" },
                new SelectListItem { Value = "Email", Text = "Correo Electrónico" },
                new SelectListItem { Value = "Phone", Text = "Teléfono" },
                new SelectListItem { Value = "DateOfBirth", Text = "Fecha de Nacimiento" },
                new SelectListItem { Value = "Photo", Text = "Foto" }
            };
            return View(institution);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var institution = await GetCurrentInstitutionAsync();
        if (institution == null)
        {
            return NotFound();
        }

        return View(institution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Institution institution)
    {
        var currentInstitution = await GetCurrentInstitutionAsync();
        if (currentInstitution == null)
        {
            return NotFound();
        }

        // Asegurar que solo puede editar SU institución (multi-tenant estricto)
        if (institution.Id != currentInstitution.Id)
        {
            _logger.LogWarning("Attempt to edit different institution. User: {UserId}, Requested: {RequestedId}, Own: {OwnId}",
                _userManager.GetUserId(User), institution.Id, currentInstitution.Id);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "No tiene permiso para editar esta institución." });
            }
            
            TempData["ErrorMessage"] = "No tiene permiso para editar esta institución.";
            return RedirectToAction(nameof(Index));
        }

        // Remover campos que no debe editar InstitutionAdmin
        ModelState.Remove(nameof(institution.CardPrefix));
        ModelState.Remove(nameof(institution.InstitutionType));
        ModelState.Remove(nameof(institution.IsActive));
        ModelState.Remove(nameof(institution.LogoPath)); // Logo se maneja por separado

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
            // Actualizar solo los campos permitidos
            currentInstitution.Name = institution.Name;
            currentInstitution.Description = institution.Description;
            currentInstitution.Email = institution.Email;
            currentInstitution.Phone = institution.Phone;
            currentInstitution.Address = institution.Address;

            // Manejar upload de logo si se proporciona
            var logoFile = Request.Form.Files["logoFile"];
            if (logoFile != null && logoFile.Length > 0)
            {
                // Validar tipo de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
                var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("LogoFile", "Solo se permiten archivos de imagen (JPG, JPEG, PNG, GIF, SVG).");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Solo se permiten archivos de imagen." });
                    }
                    return View(institution);
                }

                // Validar tamaño (máximo 5MB)
                if (logoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("LogoFile", "El archivo no puede exceder 5MB.");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "El archivo no puede exceder 5MB." });
                    }
                    return View(institution);
                }

                // Crear directorio si no existe
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generar nombre único
                var fileName = $"{currentInstitution.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);
                var relativePath = $"/uploads/logos/{fileName}";

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                // Eliminar logo anterior si existe
                if (!string.IsNullOrEmpty(currentInstitution.LogoPath) && currentInstitution.LogoPath.StartsWith("/uploads/logos/"))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", currentInstitution.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                currentInstitution.LogoPath = relativePath;
            }

            await _institutionService.UpdateAsync(currentInstitution);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                currentInstitution.Id,
                userId,
                "UPDATE",
                "Institution",
                currentInstitution.Id.ToString(),
                new Dictionary<string, object> { { "Name", currentInstitution.Name }, { "UpdatedBy", "InstitutionAdmin" } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Datos de la institución actualizados exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Datos de la institución actualizados exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating institution data");
            var errorMsg = "Error al actualizar los datos de la institución.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(institution);
        }
    }

    private async Task<Institution?> GetCurrentInstitutionAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            _logger.LogInformation("GetCurrentInstitutionAsync called. UserId: {UserId}", userId);

            // Si es SuperAdmin, no puede acceder a esta configuración (solo para InstitutionAdmin)
            // Esto ya se maneja en Index, pero lo validamos aquí también para seguridad
            if (_tenantProvider.IsSuperAdmin())
            {
                _logger.LogWarning("SuperAdmin attempted to access InstitutionConfig. User: {UserId}", userId);
                return null;
            }
            
            // Verificar que el usuario tenga rol InstitutionAdmin
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in GetCurrentInstitutionAsync. UserId: {UserId}", userId);
                return null;
            }
            
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(Roles.InstitutionAdmin))
            {
                _logger.LogWarning("User {UserId} does not have InstitutionAdmin role. Roles: {Roles}", 
                    userId, string.Join(", ", userRoles));
                return null;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogInformation("TenantId from provider: {TenantId}", tenantId);
            
            // Si no hay tenantId en los claims, intentar obtenerlo directamente del usuario
            if (!tenantId.HasValue)
            {
                _logger.LogInformation("No tenantId in claims, trying to get from user entity...");
                // user ya fue obtenido arriba, reutilizar
                if (user != null && user.InstitutionId.HasValue)
                {
                    _logger.LogInformation("User found. InstitutionId in entity: {InstitutionId}", user.InstitutionId);
                    tenantId = user.InstitutionId.Value;
                    
                    // Agregar el claim si no existe
                    var existingClaims = await _userManager.GetClaimsAsync(user);
                    var institutionClaim = existingClaims.FirstOrDefault(c => c.Type == "InstitutionId");
                    if (institutionClaim == null)
                    {
                        _logger.LogInformation("Adding InstitutionId claim to user. InstitutionId: {InstitutionId}", tenantId.Value);
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("InstitutionId", tenantId.Value.ToString()));
                        // Refrescar el sign-in para incluir el claim en la sesión actual
                        await _signInManager.RefreshSignInAsync(user);
                        _logger.LogInformation("Claim added and sign-in refreshed");
                    }
                    else
                    {
                        _logger.LogInformation("InstitutionId claim already exists: {ClaimValue}", institutionClaim.Value);
                    }
                }
                else
                {
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found in UserManager", userId);
                    }
                    else
                    {
                        _logger.LogWarning("User {UserId} does not have InstitutionId in entity", userId);
                    }
                }
            }

            if (!tenantId.HasValue)
            {
                _logger.LogWarning("User {UserId} does not have an InstitutionId. Cannot access InstitutionConfig.", userId);
                return null;
            }

            _logger.LogInformation("Fetching institution with Id: {InstitutionId}", tenantId.Value);
            var institution = await _institutionService.GetByIdAsync(tenantId.Value);
            
            if (institution == null)
            {
                _logger.LogWarning("Institution with Id {InstitutionId} not found", tenantId.Value);
            }
            else
            {
                _logger.LogInformation("Institution found: {InstitutionName}", institution.Name);
            }

            return institution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCurrentInstitutionAsync");
            return null;
        }
    }
}

