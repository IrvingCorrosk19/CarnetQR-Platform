using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarnetQRPlatform.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("Login attempt for email: {Email}", model.Email);
        
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }
        
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {Email} is not active", model.Email);
            ModelState.AddModelError(string.Empty, "Su cuenta está desactivada. Contacte al administrador.");
            return View(model);
        }
        
        // VALIDACIÓN CRÍTICA: Verificar que la institución del usuario esté activa (si tiene institución)
        if (user.InstitutionId.HasValue)
        {
            var institution = await _context.Institutions.FindAsync(user.InstitutionId.Value);
            if (institution != null && !institution.IsActive)
            {
                _logger.LogWarning("Login failed: User {Email} belongs to inactive institution {InstitutionName}", 
                    model.Email, institution.Name);
                ModelState.AddModelError(string.Empty, 
                    $"Su institución '{institution.Name}' está desactivada. Contacte al administrador del sistema.");
                return View(model);
            }
        }
        
        _logger.LogInformation("User found: {Email}, UserName: {UserName}, InstitutionId: {InstitutionId}, IsActive: {IsActive}", 
            user.Email, user.UserName, user.InstitutionId, user.IsActive);

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Login successful for user: {Email}", model.Email);
            
            // Add InstitutionId claim if not already present and user has an institution
            if (user.InstitutionId.HasValue)
            {
                var existingClaims = await _userManager.GetClaimsAsync(user);
                var institutionClaim = existingClaims.FirstOrDefault(c => c.Type == "InstitutionId");
                
                if (institutionClaim == null)
                {
                    await _userManager.AddClaimAsync(user, new Claim("InstitutionId", user.InstitutionId.Value.ToString()));
                    // Refresh sign-in to include the new claim in the current session
                    await _signInManager.RefreshSignInAsync(user);
                }
                else if (institutionClaim.Value != user.InstitutionId.Value.ToString())
                {
                    // Update claim if InstitutionId changed
                    await _userManager.RemoveClaimAsync(user, institutionClaim);
                    await _userManager.AddClaimAsync(user, new Claim("InstitutionId", user.InstitutionId.Value.ToString()));
                    await _signInManager.RefreshSignInAsync(user);
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in.", model.Email);

            // Redirigir según rol después del login
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirección según rol
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Domain.Constants.Roles.SuperAdmin))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (roles.Contains(Domain.Constants.Roles.InstitutionAdmin))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (roles.Contains(Domain.Constants.Roles.Staff))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account {Email} locked out.", model.Email);
            return View("Lockout");
        }
        
        if (result.RequiresTwoFactor)
        {
            _logger.LogWarning("User account {Email} requires two-factor authentication.", model.Email);
            ModelState.AddModelError(string.Empty, "Se requiere autenticación de dos factores.");
            return View(model);
        }
        
        if (result.IsNotAllowed)
        {
            _logger.LogWarning("User account {Email} is not allowed to sign in.", model.Email);
            ModelState.AddModelError(string.Empty, "No se permite el inicio de sesión para esta cuenta. Verifique su correo electrónico.");
            return View(model);
        }

        _logger.LogWarning("Login failed for user {Email}. Succeeded: {Succeeded}, IsLockedOut: {IsLockedOut}, RequiresTwoFactor: {RequiresTwoFactor}, IsNotAllowed: {IsNotAllowed}", 
            model.Email, result.Succeeded, result.IsLockedOut, result.RequiresTwoFactor, result.IsNotAllowed);
        
        ModelState.AddModelError(string.Empty, "Credenciales inválidas. Verifique su correo electrónico y contraseña.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        _logger.LogWarning("Access denied. User: {UserId}, ReturnUrl: {ReturnUrl}", 
            User.Identity?.Name ?? "Anonymous", returnUrl);
        
        ViewData["ReturnUrl"] = returnUrl;
        
        // Si el usuario está autenticado, obtener sus roles
        if (User.Identity?.IsAuthenticated == true)
        {
            ViewData["UserRoles"] = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
        
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {Email} changed password.", user.Email);
            ViewData["SuccessMessage"] = "Contraseña cambiada exitosamente.";
            return View();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        Institution? institution = null;
        if (user.InstitutionId != Guid.Empty)
        {
            institution = await _context.Institutions.FindAsync(user.InstitutionId);
        }

        var model = new ProfileViewModel
        {
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            InstitutionName = institution?.Name,
            LastLoginAt = user.LastLoginAt
        };

        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
