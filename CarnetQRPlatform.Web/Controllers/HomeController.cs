using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarnetQRPlatform.Web.Models;
using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Domain.Enums;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var isSuperAdmin = _tenantProvider.IsSuperAdmin();
            var tenantId = _tenantProvider.GetCurrentTenantId();

            var model = new HomeViewModel
            {
                IsSuperAdmin = isSuperAdmin
            };

            // Obtener estadísticas con filtro de tenant
            if (isSuperAdmin)
            {
                // SuperAdmin ve todas las instituciones
                model.TotalEntities = await _context.EntityProfiles.CountAsync();
                model.ActiveCards = await _context.Cards.Where(c => c.IsActive).CountAsync();
                model.ScheduledEvents = await _context.EventRecords
                    .Where(e => e.Status == EventStatus.Scheduled && e.ScheduledAt >= DateTime.UtcNow)
                    .CountAsync();
                model.TotalInstitutions = await _context.Institutions.Where(i => i.IsActive).CountAsync();

                // Calcular tasa de cumplimiento (solo eventos completados o no completados, no programados)
                var completedEvents = await _context.EventRecords
                    .Where(e => e.Status == EventStatus.Completed)
                    .CountAsync();
                var notCompletedEvents = await _context.EventRecords
                    .Where(e => e.Status == EventStatus.NotCompleted)
                    .CountAsync();
                var totalAttendedEvents = completedEvents + notCompletedEvents;

                model.CompletionRate = totalAttendedEvents > 0
                    ? Math.Round((completedEvents * 100.0) / totalAttendedEvents, 1)
                    : 0;

                // Eventos recientes (últimos 10)
                model.RecentEvents = await _context.EventRecords
                    .Include(e => e.EntityProfile)
                    .Include(e => e.Institution)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Eventos próximos (próximos 5)
                model.UpcomingEvents = await _context.EventRecords
                    .Include(e => e.EntityProfile)
                    .Include(e => e.Institution)
                    .Where(e => e.ScheduledAt >= DateTime.UtcNow && e.Status == EventStatus.Scheduled)
                    .OrderBy(e => e.ScheduledAt)
                    .Take(5)
                    .ToListAsync();

                // Carnets recientes (últimos 10)
                model.RecentCards = await _context.Cards
                    .Include(c => c.EntityProfile)
                    .Include(c => c.Institution)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            else if (tenantId.HasValue)
            {
                // InstitutionAdmin/Staff ven solo su institución
                model.TotalEntities = await _context.EntityProfiles
                    .Where(e => e.InstitutionId == tenantId.Value)
                    .CountAsync();
                model.ActiveCards = await _context.Cards
                    .Where(c => c.InstitutionId == tenantId.Value && c.IsActive)
                    .CountAsync();
                model.ScheduledEvents = await _context.EventRecords
                    .Where(e => e.InstitutionId == tenantId.Value && 
                                e.Status == EventStatus.Scheduled && 
                                e.ScheduledAt >= DateTime.UtcNow)
                    .CountAsync();

                // Calcular tasa de cumplimiento para la institución
                var completedEvents = await _context.EventRecords
                    .Where(e => e.InstitutionId == tenantId.Value && e.Status == EventStatus.Completed)
                    .CountAsync();
                var notCompletedEvents = await _context.EventRecords
                    .Where(e => e.InstitutionId == tenantId.Value && e.Status == EventStatus.NotCompleted)
                    .CountAsync();
                var totalAttendedEvents = completedEvents + notCompletedEvents;

                model.CompletionRate = totalAttendedEvents > 0
                    ? Math.Round((completedEvents * 100.0) / totalAttendedEvents, 1)
                    : 0;

                // Eventos recientes (últimos 10 de la institución)
                model.RecentEvents = await _context.EventRecords
                    .Include(e => e.EntityProfile)
                    .Include(e => e.Institution)
                    .Where(e => e.InstitutionId == tenantId.Value)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Eventos próximos (próximos 5 de la institución)
                model.UpcomingEvents = await _context.EventRecords
                    .Include(e => e.EntityProfile)
                    .Include(e => e.Institution)
                    .Where(e => e.InstitutionId == tenantId.Value && 
                                e.ScheduledAt >= DateTime.UtcNow && 
                                e.Status == EventStatus.Scheduled)
                    .OrderBy(e => e.ScheduledAt)
                    .Take(5)
                    .ToListAsync();

                // Carnets recientes (últimos 10 de la institución)
                model.RecentCards = await _context.Cards
                    .Include(c => c.EntityProfile)
                    .Include(c => c.Institution)
                    .Where(c => c.InstitutionId == tenantId.Value)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            else
            {
                // Usuario sin institución asignada (no debería pasar, pero manejamos el caso)
                _logger.LogWarning("User {UserId} accessing dashboard without InstitutionId", User.Identity?.Name);
                model.TotalEntities = 0;
                model.ActiveCards = 0;
                model.ScheduledEvents = 0;
                model.CompletionRate = 0;
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            // Retornar modelo vacío en caso de error para evitar crash
            return View(new HomeViewModel());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
