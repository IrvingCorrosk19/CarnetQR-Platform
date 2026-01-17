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
        _logger.LogInformation("[Events/Create POST] Iniciando creación de evento. EntityProfileId={EntityProfileId}, User={User}, IsSuperAdmin={IsSuperAdmin}", 
            eventRecord.EntityProfileId, User.Identity?.Name, User.IsInRole(Roles.SuperAdmin));
        
        // Remover InstitutionId e Institution del ModelState para evitar validación automática
        // Lo estableceremos desde el EntityProfile seleccionado
        ModelState.Remove(nameof(eventRecord.InstitutionId));
        ModelState.Remove(nameof(eventRecord.Institution)); // Remover también la propiedad de navegación
        ModelState.Remove(nameof(eventRecord.EntityProfile)); // Remover la propiedad de navegación EntityProfile
        
        // Validar EntityProfileId antes de continuar
        if (eventRecord.EntityProfileId == Guid.Empty)
        {
            _logger.LogWarning("[Events/Create POST] EntityProfileId está vacío.");
            await LoadEntityProfilesForView();
            
            var errorMsg = "Debe seleccionar una entidad.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError(nameof(eventRecord.EntityProfileId), errorMsg);
            return View(eventRecord);
        }
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[Events/Create POST] ModelState inválido. Errores: {Errors}", 
                string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            
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
            // Obtener EntityProfile para establecer InstitutionId
            _logger.LogInformation("[Events/Create POST] Obteniendo EntityProfile. EntityProfileId={EntityProfileId}", eventRecord.EntityProfileId);
            var entityProfile = await _entityProfileService.GetByIdAsync(eventRecord.EntityProfileId);
            if (entityProfile == null)
            {
                _logger.LogWarning("[Events/Create POST] EntityProfile no encontrado. EntityProfileId={EntityProfileId}", eventRecord.EntityProfileId);
                await LoadEntityProfilesForView();
                
                var errorMsg = "La entidad seleccionada no existe.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMsg });
                }
                
                ModelState.AddModelError(nameof(eventRecord.EntityProfileId), errorMsg);
                return View(eventRecord);
            }
            
            // Establecer InstitutionId desde el EntityProfile
            // Esto es necesario especialmente para SuperAdmin (el servicio requiere InstitutionId)
            eventRecord.InstitutionId = entityProfile.InstitutionId;
            _logger.LogInformation("[Events/Create POST] InstitutionId establecido desde EntityProfile. InstitutionId={InstitutionId}, EntityProfileId={EntityProfileId}, EntityName={EntityName}", 
                eventRecord.InstitutionId, eventRecord.EntityProfileId, $"{entityProfile.FirstName} {entityProfile.LastName}");
            
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
            _logger.LogError(ex, "[Events/Create POST] Error al crear evento. EntityProfileId={EntityProfileId}, InstitutionId={InstitutionId}", 
                eventRecord.EntityProfileId, eventRecord.InstitutionId);
            
            var errorMsg = $"Error al crear el evento: {ex.Message}";
            
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

    public async Task<IActionResult> Edit(Guid id)
    {
        var eventRecord = await _eventService.GetByIdAsync(id);
        if (eventRecord == null)
        {
            return NotFound();
        }

        // No permitir editar eventos completados
        if (eventRecord.Status != EventStatus.Scheduled)
        {
            TempData["ErrorMessage"] = "Solo se pueden editar eventos programados.";
            return RedirectToAction(nameof(Index));
        }

        // Cargar entidades según el rol del usuario
        await LoadEntityProfilesForView();

        return View(eventRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EventRecord eventRecord)
    {
        if (id != eventRecord.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "ID no coincide." });
            }
            return NotFound();
        }

        var existingEvent = await _eventService.GetByIdAsync(id);
        if (existingEvent == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Evento no encontrado." });
            }
            return NotFound();
        }

        // No permitir editar eventos completados
        if (existingEvent.Status != EventStatus.Scheduled)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Solo se pueden editar eventos programados." });
            }
            TempData["ErrorMessage"] = "Solo se pueden editar eventos programados.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
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
            // Actualizar campos editables
            existingEvent.EntityProfileId = eventRecord.EntityProfileId;
            existingEvent.ScheduledAt = eventRecord.ScheduledAt;
            existingEvent.Notes = eventRecord.Notes;

            // Usar UpdateStatusAsync para actualizar (aunque no cambiemos el status)
            // O mejor, necesitamos un método UpdateAsync en el servicio
            // Por ahora, voy a usar el contexto directamente o agregar UpdateAsync al servicio
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                existingEvent.InstitutionId,
                userId,
                "UPDATE",
                "EventRecord",
                existingEvent.Id.ToString(),
                new Dictionary<string, object> 
                { 
                    { "ScheduledAt", existingEvent.ScheduledAt }, 
                    { "EntityProfileId", existingEvent.EntityProfileId.ToString() },
                    { "Notes", existingEvent.Notes ?? "" }
                });

            // Necesitamos actualizar el evento - voy a verificar si hay UpdateAsync
            // Si no existe, lo agregaremos al servicio
            var updated = await _eventService.UpdateAsync(existingEvent);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Evento actualizado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }

            TempData["SuccessMessage"] = "Evento actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event");
            var errorMsg = $"Error al actualizar el evento: {ex.Message}";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            await LoadEntityProfilesForView();
            ModelState.AddModelError("", errorMsg);
            return View(eventRecord);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
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

            // Cambiar entre Scheduled y NotCompleted (desactivar/activar)
            // Solo se puede desactivar eventos programados
            EventStatus newStatus;
            if (eventRecord.Status == EventStatus.Scheduled)
            {
                newStatus = EventStatus.NotCompleted;
            }
            else if (eventRecord.Status == EventStatus.NotCompleted)
            {
                newStatus = EventStatus.Scheduled;
            }
            else
            {
                // No se puede cambiar el estado de eventos completados
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se puede cambiar el estado de eventos completados." });
                }
                TempData["ErrorMessage"] = "No se puede cambiar el estado de eventos completados.";
                return RedirectToAction(nameof(Index));
            }

            var updated = await _eventService.UpdateStatusAsync(id, newStatus);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                updated.InstitutionId,
                userId,
                "TOGGLE_ACTIVE",
                "EventRecord",
                updated.Id.ToString(),
                new Dictionary<string, object> 
                { 
                    { "OldStatus", eventRecord.Status.ToString() }, 
                    { "NewStatus", newStatus.ToString() } 
                });

            var message = newStatus == EventStatus.Scheduled 
                ? "Evento activado exitosamente." 
                : "Evento desactivado exitosamente.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = message, status = newStatus.ToString() });
            }

            TempData["SuccessMessage"] = message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling event active status");
            var errorMsg = "Error al cambiar el estado del evento.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
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

            var institutionId = eventRecord.InstitutionId;
            var eventId = eventRecord.Id;
            var deleted = await _eventService.DeleteAsync(id);

            if (deleted)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    institutionId,
                    userId,
                    "DELETE",
                    "EventRecord",
                    eventId.ToString(),
                    new Dictionary<string, object> { { "ScheduledAt", eventRecord.ScheduledAt }, { "Status", eventRecord.Status.ToString() } });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Evento eliminado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Evento eliminado exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar el evento." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar el evento.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event");
            var errorMsg = $"Error al eliminar el evento: {ex.Message}";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}

