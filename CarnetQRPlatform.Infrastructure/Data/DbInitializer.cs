using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Domain.Enums;
using CarnetQRPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CarnetQRPlatform.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Starting database initialization...");
            
            await context.Database.MigrateAsync();
            logger?.LogInformation("Database migrations completed.");
            
            await SeedRolesAsync(roleManager);
            logger?.LogInformation("Roles seeded.");
            
            await SeedSuperAdminAsync(context, userManager, logger);
            await SeedDemoInstitutionAsync(context, userManager, logger);
            
            logger?.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in DbInitializer: {Message}", ex.Message);
            throw; // Re-throw to see the error
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Staff, Roles.AdministrativeOperator };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task SeedSuperAdminAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger? logger = null)
    {
        var superAdminEmail = "admin@qlservices.com";
        var existingUser = await userManager.FindByEmailAsync(superAdminEmail);

        if (existingUser == null)
        {
            var superAdmin = new AppUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                InstitutionId = null, // SuperAdmin doesn't belong to any institution
                IsActive = true,
                EmailConfirmed = true,
                LockoutEnabled = false
            };

            var result = await userManager.CreateAsync(superAdmin, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                System.Diagnostics.Debug.WriteLine($"Error creating SuperAdmin: {errors}");
            }
        }
    }

    private static async Task SeedDemoInstitutionAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger? logger = null)
    {
        var demoInstitution = await context.Institutions
            .FirstOrDefaultAsync(i => i.Name == "Empresa Demo");

        if (demoInstitution == null)
        {
            demoInstitution = new Institution
            {
                Name = "Empresa Demo",
                Description = "Empresa de demostración",
                CardPrefix = "DEMO",
                InstitutionType = InstitutionType.Clinica,
                IsActive = true
            };

            context.Institutions.Add(demoInstitution);
            await context.SaveChangesAsync();

            // Inicializar templates predefinidos para la institución demo
            try
            {
                var templateInitializer = new CardTemplateInitializer(context);
                await templateInitializer.InitializeDefaultTemplatesAsync(demoInstitution.Id);
                logger?.LogInformation("Default card templates initialized for demo institution.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not initialize default card templates for demo institution: {Message}", ex.Message);
            }

            var adminEmail = "admin@demo.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var institutionAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "Demo",
                    InstitutionId = demoInstitution.Id,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(institutionAdmin, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(institutionAdmin, Roles.InstitutionAdmin);
                    logger?.LogInformation("InstitutionAdmin user created successfully: {Email}", adminEmail);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger?.LogError("Error creating InstitutionAdmin: {Errors}", errors);
                }
            }
        }
        else
        {
            // Si la institución ya existe, verificar si tiene templates y crearlos si no existen
            var hasTemplates = await context.CardTemplates
                .AnyAsync(t => t.InstitutionId == demoInstitution.Id);

            if (!hasTemplates)
            {
                try
                {
                    var templateInitializer = new CardTemplateInitializer(context);
                    await templateInitializer.InitializeDefaultTemplatesAsync(demoInstitution.Id);
                    logger?.LogInformation("Default card templates initialized for existing demo institution.");
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Could not initialize default card templates for existing demo institution: {Message}", ex.Message);
                }
            }
        }
    }
}


