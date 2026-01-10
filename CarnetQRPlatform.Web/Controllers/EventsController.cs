using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly IEventService _eventService;
    private readonly IEntityProfileService _entityProfileService;
    private readonly IInstitutionService _institutionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _auditService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        IEntityProfileService entityProfileService,
        IInstitutionService institutionService,
        ITenantProvider tenantProvider,
        IAuditService auditService,
        UserManager<AppUser> userManager,
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _entityProfileService = entityProfileService;
        _institutionService = institutionService;
        _tenantProvider = tenantProvider;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _eventService.GetAllAsync();
        return View(events);
    }

    public async Task<IActionResult> Create(Guid? entityProfileId)
    {
        var eventRecord = new EventRecord
        {
            EntityProfileId = entityProfileId ?? Guid.Empty,
            ScheduledAt = DateTime.UtcNow.AddDays(1)
        };

        // Cargar entidades según el rol del usuario
        await LoadEntityProfilesForView();

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventRecord eventRecord)
    {
        if (!ModelState.IsValid)
        {
            // Recargar entidades si hay error de validación
            await LoadEntityProfilesForView();
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(eventRecord);
        }

        try
        {
            var created = await _eventService.CreateAsync(eventRecord);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                created.InstitutionId,
                userId,
                "CREATE",
                "EventRecord",
                created.Id.ToString(),
                new Dictionary<string, object> { { "ScheduledAt", created.ScheduledAt }, { "Status", created.Status.ToString() } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Evento creado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Evento creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            var errorMsg = "Error al crear el evento.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            // Recargar entidades si hay error
            await LoadEntityProfilesForView();
            
            ModelState.AddModelError("", errorMsg);
            return View(eventRecord);
        }
    }

    private async Task LoadEntityProfilesForView()
    {
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        
        if (isSuperAdmin)
        {
            // SuperAdmin puede ver todas las instituciones y entidades
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            
            // Cargar todas las entidades de todas las instituciones
            var allEntities = await _entityProfileService.GetAllAsync();
            ViewBag.EntityProfiles = allEntities.Where(e => e.IsActive).OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToList();
        }
        else
        {
            // InstitutionAdmin, Staff, etc. solo ven entidades de su institución
            var entities = await _entityProfileService.GetAllAsync();
            ViewBag.EntityProfiles = entities.Where(e => e.IsActive).OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToList();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, EventStatus status)
    {
        // Validar permisos: Solo Staff e InstitutionAdmin pueden marcar atención
        // AdministrativeOperator NO puede marcar atención según especificación
        if (status != EventStatus.Scheduled && !User.IsInRole(Roles.Staff) && !User.IsInRole(Roles.InstitutionAdmin) && !User.IsInRole(Roles.SuperAdmin))
        {
            var errorMsg = "No tiene permisos para marcar atención. Solo funcionarios de salud y administradores pueden realizar esta acción.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Index));
        }
        
        try
        {
            var eventRecord = await _eventService.GetByIdAsync(id);
            if (eventRecord == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Evento no encontrado." });
                }
                return NotFound();
            }
            
            var updated = await _eventService.UpdateStatusAsync(id, status);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                updated.InstitutionId,
                userId,
                "UPDATE_STATUS",
                "EventRecord",
                updated.Id.ToString(),
                new Dictionary<string, object> { { "OldStatus", eventRecord.Status.ToString() }, { "NewStatus", status.ToString() } });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Estado del evento actualizado exitosamente." });
            }
            
            TempData["SuccessMessage"] = "Estado del evento actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event status");
            var errorMsg = "Error al actualizar el estado del evento.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Index));
        }
    }
}

