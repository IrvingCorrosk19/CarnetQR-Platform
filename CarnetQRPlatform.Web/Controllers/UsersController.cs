using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize(Policy = "InstitutionAdminOrAbove")]
public class UsersController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IInstitutionService _institutionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IInstitutionService institutionService,
        ITenantProvider tenantProvider,
        IAuditService auditService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _institutionService = institutionService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        IQueryable<AppUser> usersQuery = _userManager.Users.Include(u => u.Institution);

        // Si es InstitutionAdmin, filtrar solo usuarios de su institución
        if (!isSuperAdmin)
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.InstitutionId == tenantId.Value);
            }
            else
            {
                // Si no hay tenant, no mostrar usuarios
                usersQuery = usersQuery.Where(u => false);
            }
        }

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var usersWithRoles = new List<object>();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new
            {
                User = user,
                Roles = userRoles
            });
        }

        ViewBag.UsersWithRoles = usersWithRoles;
        ViewBag.IsSuperAdmin = isSuperAdmin;
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        
        // Si es InstitutionAdmin, solo puede crear usuarios para su institución
        if (!isSuperAdmin)
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                TempData["ErrorMessage"] = "No tiene una institución asignada.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.InstitutionId = tenantId.Value;
            var institution = await _institutionService.GetByIdAsync(tenantId.Value);
            ViewBag.InstitutionName = institution?.Name;
        }
        else
        {
            // SuperAdmin puede seleccionar institución
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        }
        
        // InstitutionAdmin no puede crear otros InstitutionAdmin ni SuperAdmin
        var availableRoles = isSuperAdmin 
            ? new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator }
            : new[] { Roles.Staff, Roles.AdministrativeOperator };
        ViewBag.AvailableRoles = availableRoles;
        ViewBag.IsSuperAdmin = isSuperAdmin;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        System.Console.WriteLine("=== [UsersController] Create ===");
        System.Console.WriteLine($"Email: {model.Email}, Role: {model.Role}, InstitutionId: {model.InstitutionId}");

        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        
        // Si es InstitutionAdmin, forzar su institución
        if (!isSuperAdmin)
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                ModelState.AddModelError("", "No tiene una institución asignada.");
            }
            else
            {
                model.InstitutionId = tenantId.Value;
            }
            
            // InstitutionAdmin no puede crear otros InstitutionAdmin ni SuperAdmin
            if (model.Role == Roles.SuperAdmin || model.Role == Roles.InstitutionAdmin)
            {
                ModelState.AddModelError(nameof(model.Role), "No tiene permisos para crear este tipo de usuario.");
            }
        }
        else
        {
            // Validar que se seleccionó una institución si el rol no es SuperAdmin
            if (model.Role != Roles.SuperAdmin && model.InstitutionId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.InstitutionId), "Debe seleccionar una empresa para este rol.");
            }

            // Validar que SuperAdmin no tenga institución
            if (model.Role == Roles.SuperAdmin && model.InstitutionId != Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.InstitutionId), "El SuperAdmin no puede tener una empresa asignada.");
            }
        }

        if (!ModelState.IsValid)
        {
            if (isSuperAdmin)
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            else
            {
                var tenantId = _tenantProvider.GetCurrentTenantId();
                if (tenantId.HasValue)
                {
                    ViewBag.InstitutionId = tenantId.Value;
                    var institution = await _institutionService.GetByIdAsync(tenantId.Value);
                    ViewBag.InstitutionName = institution?.Name;
                }
            }
            
            var availableRoles = isSuperAdmin 
                ? new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator }
                : new[] { Roles.Staff, Roles.AdministrativeOperator };
            ViewBag.AvailableRoles = availableRoles;
            ViewBag.IsSuperAdmin = isSuperAdmin;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(model);
        }

        try
        {
            // Validar que la institución existe y está activa (si se proporcionó)
            if (model.InstitutionId != Guid.Empty)
            {
                var institution = await _institutionService.GetByIdAsync(model.InstitutionId);
                if (institution == null || !institution.IsActive)
                {
                    ModelState.AddModelError(nameof(model.InstitutionId), "La empresa seleccionada no existe o está inactiva.");
                    var institutions = await _institutionService.GetAllAsync();
                    ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                    var availableRoles = new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator };
                    ViewBag.AvailableRoles = availableRoles;

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "La empresa seleccionada no existe o está inactiva." });
                    }
                    return View(model);
                }
            }

            // Verificar si el email ya existe
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Este correo electrónico ya está registrado.");
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                var availableRoles = new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator };
                ViewBag.AvailableRoles = availableRoles;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Este correo electrónico ya está registrado." });
                }
                return View(model);
            }

            // Crear el usuario
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                InstitutionId = model.InstitutionId != Guid.Empty ? model.InstitutionId : null,
                IsActive = true,
                EmailConfirmed = true
            };

            System.Console.WriteLine($"Creating user: {user.Email}, InstitutionId: {user.InstitutionId}");

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                System.Console.WriteLine("User created successfully, assigning role...");
                
                // Asignar rol
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        System.Console.WriteLine($"Error assigning role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        _logger.LogWarning("User created but role assignment failed: {Errors}", 
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }

                // Agregar claim de InstitutionId si tiene institución
                if (user.InstitutionId.HasValue)
                {
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("InstitutionId", user.InstitutionId.Value.ToString()));
                }

                System.Console.WriteLine("User creation completed successfully");
                _logger.LogInformation("User {Email} created with role {Role} and InstitutionId {InstitutionId}", 
                    user.Email, model.Role, user.InstitutionId);

                // Registrar auditoría
                if (user.InstitutionId.HasValue)
                {
                    var currentUserId = _userManager.GetUserId(User);
                    await _auditService.LogActionAsync(
                        user.InstitutionId.Value,
                        currentUserId,
                        "CREATE",
                        "AppUser",
                        user.Id,
                        new Dictionary<string, object> { { "Email", user.Email }, { "Role", model.Role } });
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Usuario creado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                System.Console.WriteLine($"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                var availableRoles = new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator };
                ViewBag.AvailableRoles = availableRoles;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return Json(new { success = false, message = string.Join(" ", errors) });
                }
                return View(model);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Exception creating user: {ex.Message}");
            System.Console.WriteLine($"StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error creating user");
            var errorMsg = "Error al crear el usuario.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            ModelState.AddModelError("", errorMsg);
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            var availableRoles = new[] { Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator };
            ViewBag.AvailableRoles = availableRoles;
            return View(model);
        }
        finally
        {
            System.Console.WriteLine("=== [UsersController] Create END ===");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Usuario no encontrado." });
            }
            return NotFound();
        }

        // No permitir desactivar al SuperAdmin actual
        if (user.Id == _userManager.GetUserId(User))
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "No puede desactivar su propio usuario." });
            }
            TempData["ErrorMessage"] = "No puede desactivar su propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        var oldStatus = user.IsActive;
        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            // Registrar auditoría
            if (user.InstitutionId.HasValue)
            {
                var currentUserId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    user.InstitutionId.Value,
                    currentUserId,
                    "TOGGLE_ACTIVE",
                    "AppUser",
                    user.Id,
                    new Dictionary<string, object> { { "Email", user.Email ?? "" }, { "OldStatus", oldStatus }, { "NewStatus", user.IsActive } });
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Estado del usuario actualizado." });
            }
            TempData["SuccessMessage"] = "Estado del usuario actualizado.";
        }
        else
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Error al actualizar el estado." });
            }
            TempData["ErrorMessage"] = "Error al actualizar el estado.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "Usuario no encontrado." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            return Json(new { success = true, message = "Contraseña restablecida exitosamente." });
        }

        var errors = string.Join(" ", result.Errors.Select(e => e.Description));
        return Json(new { success = false, message = errors });
    }
}

