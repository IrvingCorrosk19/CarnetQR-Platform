using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarnetQRPlatform.Infrastructure.Data;

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
            usersWithRoles.Add(new
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                InstitutionId = user.InstitutionId,
                IsActive = user.IsActive,
                Roles = userRoles
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
}

