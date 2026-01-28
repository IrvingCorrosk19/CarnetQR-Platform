using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Constants;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarnetQRPlatform.Web.Controllers;

[Authorize]
public class DoctorsController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly ISpecialtyService _specialtyService;
    private readonly IInstitutionService _institutionService;
    private readonly IAuditService _auditService;
    private readonly ITenantProvider _tenantProvider;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(
        IDoctorService doctorService,
        ISpecialtyService specialtyService,
        IInstitutionService institutionService,
        IAuditService auditService,
        ITenantProvider tenantProvider,
        UserManager<AppUser> userManager,
        ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _specialtyService = specialtyService;
        _institutionService = institutionService;
        _auditService = auditService;
        _tenantProvider = tenantProvider;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var doctors = await _doctorService.GetAllAsync();
        return View(doctors);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        if (doctor == null)
        {
            return NotFound();
        }

        return View(doctor);
    }

    public async Task<IActionResult> Create()
    {
        var model = new Doctor();
        
        // Cargar especialidades activas
        var specialties = await _specialtyService.GetAllAsync();
        ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        
        // Si es SuperAdmin, cargar lista de instituciones para seleccionar
        if (User.IsInRole(Roles.SuperAdmin))
        {
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        }
        else
        {
            // Para InstitutionAdmin, cargar solo su institución
            var tenantId = _tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                var institution = await _institutionService.GetByIdAsync(tenantId.Value);
                if (institution != null)
                {
                    ViewBag.Institutions = new List<Institution> { institution };
                }
            }
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Doctor doctor)
    {
        _logger.LogInformation("[Doctors/Create POST] Iniciando creación. Usuario: {User}, IsSuperAdmin: {IsSuperAdmin}, InstitutionId: {InstitutionId}, SpecialtyId: {SpecialtyId}, Nombre: {FirstName} {LastName}",
            User.Identity?.Name, User.IsInRole(Roles.SuperAdmin), doctor.InstitutionId, doctor.SpecialtyId, doctor.FirstName, doctor.LastName);

        // Remover propiedades de navegación del ModelState para evitar validación automática
        ModelState.Remove(nameof(doctor.Institution));
        ModelState.Remove(nameof(doctor.Specialty));

        try
        {
            // Si es SuperAdmin, validar que haya seleccionado una institución
            if (User.IsInRole(Roles.SuperAdmin))
            {
                _logger.LogInformation("[Doctors/Create POST] Usuario es SuperAdmin. Validando InstitutionId: {InstitutionId}", doctor.InstitutionId);
                if (doctor.InstitutionId == Guid.Empty)
                {
                    _logger.LogWarning("[Doctors/Create POST] SuperAdmin no seleccionó institución");
                    ModelState.AddModelError(nameof(doctor.InstitutionId), "Debe seleccionar una institución.");
                }
            }
            else
            {
                var tenantId = _tenantProvider.GetCurrentTenantId();
                _logger.LogInformation("[Doctors/Create POST] Usuario no-SuperAdmin. TenantId: {TenantId}", tenantId);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[Doctors/Create POST] ModelState inválido. Errores: {Errors}", string.Join(" | ", errors));
                
                // Recargar datos para la vista
                var specialties = await _specialtyService.GetAllAsync();
                ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
                
                if (User.IsInRole(Roles.SuperAdmin))
                {
                    var institutions = await _institutionService.GetAllAsync();
                    ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
                }
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = string.Join(" ", errors) });
                }
                return View(doctor);
            }

            _logger.LogInformation("[Doctors/Create POST] Llamando a DoctorService.CreateAsync. InstitutionId: {InstitutionId}, SpecialtyId: {SpecialtyId}", 
                doctor.InstitutionId, doctor.SpecialtyId);
            var createdDoctor = await _doctorService.CreateAsync(doctor);
            _logger.LogInformation("[Doctors/Create POST] Médico creado exitosamente. ID: {Id}, Nombre: {FirstName} {LastName}, InstitutionId: {InstitutionId}",
                createdDoctor.Id, createdDoctor.FirstName, createdDoctor.LastName, createdDoctor.InstitutionId);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                createdDoctor.InstitutionId,
                userId,
                "CREATE",
                "Doctor",
                createdDoctor.Id.ToString(),
                new Dictionary<string, object> { 
                    { "Name", $"{createdDoctor.FirstName} {createdDoctor.LastName}" },
                    { "Specialty", createdDoctor.Specialty?.Name ?? "" }
                });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Médico creado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Médico creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[Doctors/Create POST] Error de negocio al crear médico. InstitutionId: {InstitutionId}, SpecialtyId: {SpecialtyId}, Mensaje: {Message}",
                doctor.InstitutionId, doctor.SpecialtyId, ex.Message);
            
            // Recargar datos para la vista
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("", ex.Message);
            return View(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Doctors/Create POST] Error inesperado al crear médico. InstitutionId: {InstitutionId}, SpecialtyId: {SpecialtyId}",
                doctor.InstitutionId, doctor.SpecialtyId);
            var errorMsg = "Error al crear el médico.";
            
            // Recargar datos para la vista
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(doctor);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        if (doctor == null)
        {
            return NotFound();
        }

        // Cargar especialidades activas
        var specialties = await _specialtyService.GetAllAsync();
        ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        
        // Si es SuperAdmin, cargar lista de instituciones (aunque no se puede cambiar)
        if (User.IsInRole(Roles.SuperAdmin))
        {
            var institutions = await _institutionService.GetAllAsync();
            ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        }

        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Doctor doctor)
    {
        _logger.LogInformation("[Doctors/Edit POST] Iniciando edición. ID: {Id}, Usuario: {User}, InstitutionId: {InstitutionId}, SpecialtyId: {SpecialtyId}",
            id, User.Identity?.Name, doctor.InstitutionId, doctor.SpecialtyId);

        if (id != doctor.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "ID no coincide." });
            }
            return NotFound();
        }

        // Remover propiedades de navegación del ModelState para evitar validación automática
        ModelState.Remove(nameof(doctor.Institution));
        ModelState.Remove(nameof(doctor.Specialty));

        if (!ModelState.IsValid)
        {
            // Recargar datos para la vista
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }
            return View(doctor);
        }

        try
        {
            var updatedDoctor = await _doctorService.UpdateAsync(doctor);
            
            // Registrar auditoría
            var userId = _userManager.GetUserId(User);
            await _auditService.LogActionAsync(
                updatedDoctor.InstitutionId,
                userId,
                "UPDATE",
                "Doctor",
                updatedDoctor.Id.ToString(),
                new Dictionary<string, object> { 
                    { "Name", $"{updatedDoctor.FirstName} {updatedDoctor.LastName}" }
                });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Médico actualizado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
            }
            
            TempData["SuccessMessage"] = "Médico actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error updating doctor: {Message}", ex.Message);
            
            // Recargar datos para la vista
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            
            ModelState.AddModelError("", ex.Message);
            return View(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor");
            var errorMsg = "Error al actualizar el médico.";
            
            // Recargar datos para la vista
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = specialties.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            
            if (User.IsInRole(Roles.SuperAdmin))
            {
                var institutions = await _institutionService.GetAllAsync();
                ViewBag.Institutions = institutions.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }
            
            ModelState.AddModelError("", errorMsg);
            return View(doctor);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _doctorService.ToggleActiveAsync(id);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result)
            {
                return Json(new { success = true, message = "Estado del médico actualizado." });
            }
            else
            {
                return Json(new { success = false, message = "Error al actualizar el estado." });
            }
        }
        
        if (result)
        {
            TempData["SuccessMessage"] = "Estado del médico actualizado.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al actualizar el estado.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var doctor = await _doctorService.GetByIdAsync(id);
            if (doctor == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Médico no encontrado." });
                }
                return NotFound();
            }

            var doctorName = $"{doctor.FirstName} {doctor.LastName}";
            var institutionId = doctor.InstitutionId;
            var deleted = await _doctorService.DeleteAsync(id);

            if (deleted)
            {
                // Registrar auditoría
                var userId = _userManager.GetUserId(User);
                await _auditService.LogActionAsync(
                    institutionId,
                    userId,
                    "DELETE",
                    "Doctor",
                    id.ToString(),
                    new Dictionary<string, object> { { "Name", doctorName } });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Médico eliminado exitosamente.", redirectUrl = Url.Action(nameof(Index)) });
                }

                TempData["SuccessMessage"] = "Médico eliminado exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se pudo eliminar el médico." });
                }
                TempData["ErrorMessage"] = "No se pudo eliminar el médico.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting doctor");
            var errorMsg = $"Error al eliminar el médico: {ex.Message}";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = errorMsg });
            }

            TempData["ErrorMessage"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }
}
