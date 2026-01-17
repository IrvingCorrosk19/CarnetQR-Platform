using CarnetQRPlatform.Infrastructure;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Infrastructure.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// 👇 Forwarded headers (OBLIGATORIO en Docker / VPS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add caching services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CarnetQRPlatform.Application.Services.ICacheService, CarnetQRPlatform.Application.Services.MemoryCacheService>();

// Add QR Code Service
builder.Services.AddScoped<CarnetQRPlatform.Web.Services.QrCodeService>();

var app = builder.Build();

// 👇 USAR forwarded headers (ANTES de cualquier otro middleware)
app.UseForwardedHeaders();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
        
        // Verify users were created
        var userCount = await userManager.Users.CountAsync();
        logger.LogInformation("Database initialized. Total users: {UserCount}", userCount);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
        throw; // Re-throw to see the error in console
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 👇 NO FORZAR HTTPS (comentado temporalmente para Docker/HTTP)
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

// Rate limiting middleware (early in pipeline)
app.UseMiddleware<RateLimitMiddleware>();

app.UseRouting();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://cdn.jsdelivr.net ws://localhost:* wss://localhost:* http://localhost:*; " +
        "frame-ancestors 'none';";
    
    await next();
});

app.UseAuthentication();

// Add Tenant Middleware (debe estar después de Authentication, antes de Authorization)
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
