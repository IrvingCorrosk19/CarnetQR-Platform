using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarnetQRPlatform.Infrastructure.Data;
using System.Linq;

namespace CarnetQRPlatform.Web.Controllers;

[AllowAnonymous]
public class TestController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public TestController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> CheckUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var roles = await _roleManager.Roles.ToListAsync();
        var institutions = await _context.Institutions.ToListAsync();

        var usersWithRoles = new List<object>();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);
            usersWithRoles.Add(new
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                InstitutionId = user.InstitutionId,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                PasswordHash = user.PasswordHash != null ? "EXISTS" : "NULL",
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = userRoles,
                Claims = userClaims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
            });
        }

        var result = new
        {
            TotalUsers = users.Count,
            Users = usersWithRoles,
            TotalRoles = roles.Count,
            Roles = roles.Select(r => r.Name),
            TotalInstitutions = institutions.Count,
            Institutions = institutions.Select(i => new { i.Id, i.Name, i.CardPrefix })
        };

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> CompareUsers()
    {
        var email1 = "aloticopty@tico.com";
        var email2 = "admin@qlservices.com";
        
        var user1 = await _userManager.FindByEmailAsync(email1);
        var user2 = await _userManager.FindByEmailAsync(email2);
        
        var result = new
        {
            User1 = user1 != null ? new
            {
                Email = user1.Email,
                UserName = user1.UserName,
                NormalizedEmail = user1.NormalizedEmail,
                NormalizedUserName = user1.NormalizedUserName,
                EmailConfirmed = user1.EmailConfirmed,
                IsActive = user1.IsActive,
                InstitutionId = user1.InstitutionId,
                FirstName = user1.FirstName,
                LastName = user1.LastName,
                LockoutEnabled = user1.LockoutEnabled,
                LockoutEnd = user1.LockoutEnd,
                AccessFailedCount = user1.AccessFailedCount,
                PasswordHash = user1.PasswordHash != null ? "EXISTS" : "NULL",
                Roles = user1 != null ? await _userManager.GetRolesAsync(user1) : new List<string>(),
                Claims = user1 != null ? (await _userManager.GetClaimsAsync(user1)).Select(c => new { Type = c.Type, Value = c.Value }).Cast<object>().ToList() : new List<object>(),
                Institution = user1?.InstitutionId != null ? await _context.Institutions.FindAsync(user1.InstitutionId) : null
            } : null,
            User2 = user2 != null ? new
            {
                Email = user2.Email,
                UserName = user2.UserName,
                NormalizedEmail = user2.NormalizedEmail,
                NormalizedUserName = user2.NormalizedUserName,
                EmailConfirmed = user2.EmailConfirmed,
                IsActive = user2.IsActive,
                InstitutionId = user2.InstitutionId,
                FirstName = user2.FirstName,
                LastName = user2.LastName,
                LockoutEnabled = user2.LockoutEnabled,
                LockoutEnd = user2.LockoutEnd,
                AccessFailedCount = user2.AccessFailedCount,
                PasswordHash = user2.PasswordHash != null ? "EXISTS" : "NULL",
                Roles = user2 != null ? await _userManager.GetRolesAsync(user2) : new List<string>(),
                Claims = user2 != null ? (await _userManager.GetClaimsAsync(user2)).Select(c => new { Type = c.Type, Value = c.Value }).Cast<object>().ToList() : new List<object>(),
                Institution = user2?.InstitutionId != null ? await _context.Institutions.FindAsync(user2.InstitutionId) : null
            } : null,
            Differences = new
            {
                User1Exists = user1 != null,
                User2Exists = user2 != null,
                User1HasPassword = user1?.PasswordHash != null,
                User2HasPassword = user2?.PasswordHash != null,
                User1IsActive = user1?.IsActive ?? false,
                User2IsActive = user2?.IsActive ?? false,
                User1EmailConfirmed = user1?.EmailConfirmed ?? false,
                User2EmailConfirmed = user2?.EmailConfirmed ?? false,
                User1LockedOut = user1?.LockoutEnd != null && user1.LockoutEnd > DateTimeOffset.UtcNow,
                User2LockedOut = user2?.LockoutEnd != null && user2.LockoutEnd > DateTimeOffset.UtcNow,
                User1AccessFailedCount = user1?.AccessFailedCount ?? 0,
                User2AccessFailedCount = user2?.AccessFailedCount ?? 0
            }
        };
        
        return Json(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}

