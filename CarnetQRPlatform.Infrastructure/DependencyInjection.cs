using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace CarnetQRPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentity<Domain.Entities.AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            // 👇 FIX para Docker/HTTP (sin HTTPS)
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();

        // Application Services
        services.AddScoped<Application.Services.IInstitutionService, Infrastructure.Services.InstitutionService>();
        services.AddScoped<Application.Services.IInstitutionTypeService, Infrastructure.Services.InstitutionTypeService>();
        services.AddScoped<Application.Services.IEntityProfileService, Infrastructure.Services.EntityProfileService>();
        services.AddScoped<Application.Services.ICardService, Infrastructure.Services.CardService>();
        services.AddScoped<Application.Services.IEventService, Infrastructure.Services.EventService>();
        services.AddScoped<Application.Services.ICardTemplateService, Infrastructure.Services.CardTemplateService>();
        services.AddScoped<IAuditService, AuditService>();
        
        // Template Initializer (transient porque puede usarse fuera del contexto HTTP)
        services.AddTransient<CardTemplateInitializer>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(Roles.SuperAdmin));
            options.AddPolicy("InstitutionAdminOrAbove", policy => policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin));
            options.AddPolicy("StaffOrAbove", policy => policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Staff));
        });

        return services;
    }
}

