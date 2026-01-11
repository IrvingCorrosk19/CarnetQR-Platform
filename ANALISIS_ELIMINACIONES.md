# Análisis de Eliminaciones y Operaciones CRUD del Sistema

## Estado Actual de Operaciones CRUD

### 1. USUARIOS (UsersController)
| Operación | Estado | Notas |
|-----------|--------|-------|
| **Create** | ✅ Implementado | Funciona correctamente |
| **Read (Index)** | ✅ Implementado | Lista usuarios con roles |
| **Edit** | ❌ **FALTA** | No existe acción Edit |
| **Delete** | ❌ **FALTA** | No existe acción Delete |
| **ToggleActive** | ✅ Implementado | Desactivar/activar usuarios |

### 2. INSTITUCIONES/EMPRESAS (InstitutionsController)
| Operación | Estado | Notas |
|-----------|--------|-------|
| **Create** | ✅ Implementado | Funciona correctamente |
| **Read (Index)** | ✅ Implementado | Lista instituciones |
| **Edit** | ✅ Implementado | Funciona correctamente |
| **Delete** | ❌ **FALTA EN CONTROLADOR** | Existe en servicio pero no en controlador |
| **ToggleActive** | ✅ Implementado | Desactivar/activar instituciones |

**Validaciones necesarias antes de eliminar institución:**
- ❌ No valida si tiene usuarios asociados
- ❌ No valida si tiene entidades asociadas
- ❌ No valida si tiene carnets asociados
- ❌ No valida si tiene eventos asociados
- ❌ No valida si tiene plantillas de carnet asociadas

### 3. ENTIDADES (EntityProfilesController)
| Operación | Estado | Notas |
|-----------|--------|-------|
| **Create** | ✅ Implementado | Funciona correctamente |
| **Read (Index)** | ✅ Implementado | Lista entidades |
| **Edit** | ✅ Implementado | Funciona correctamente |
| **Delete** | ✅ Implementado | **Tiene validaciones** ✅ |
| **ToggleActive** | ❌ **FALTA** | No existe acción ToggleActive |

**Validaciones actuales en Delete:**
- ✅ Valida si tiene carnets asociados
- ✅ Valida si tiene eventos asociados

### 4. EVENTOS (EventsController)
| Operación | Estado | Notas |
|-----------|--------|-------|
| **Create** | ✅ Implementado | Funciona correctamente |
| **Read (Index)** | ✅ Implementado | Lista eventos |
| **Edit** | ❌ **FALTA** | No existe acción Edit |
| **Delete** | ❌ **FALTA EN CONTROLADOR** | Existe en servicio pero no en controlador |
| **ToggleActive** | ❌ **FALTA** | No existe acción ToggleActive |

**Nota:** Los eventos no tienen relaciones que impidan su eliminación.

### 5. CARNETS (CardsController)
| Operación | Estado | Notas |
|-----------|--------|-------|
| **Create** | ✅ Implementado | Se crea desde EntityProfiles |
| **Read (Index/Details)** | ✅ Implementado | Lista y detalles de carnets |
| **Edit** | ❌ **NO APLICA** | Los carnets no se editan, se regeneran |
| **Delete** | ❌ **FALTA EN CONTROLADOR** | Existe en servicio pero no en controlador |
| **ToggleActive** | ✅ Implementado | Desactivar/activar carnets |

**Nota:** Los carnets no tienen relaciones que impidan su eliminación.

---

## Configuración de Entity Framework (DeleteBehavior)

### Estado Actual: ✅ CORRECTO
Todas las relaciones usan `DeleteBehavior.Restrict` (NO hay cascadas):

```csharp
// ApplicationDbContext.cs
entity.HasOne(e => e.Institution)
    .WithMany(i => i.EntityProfiles)
    .HasForeignKey(e => e.InstitutionId)
    .OnDelete(DeleteBehavior.Restrict); // ✅ CORRECTO

entity.HasOne(e => e.EntityProfile)
    .WithMany(ep => ep.Cards)
    .HasForeignKey(e => e.EntityProfileId)
    .OnDelete(DeleteBehavior.Restrict); // ✅ CORRECTO

// ... todas las demás relaciones también usan Restrict
```

**✅ NO HAY ELIMINACIONES EN CASCADA** - El sistema está configurado correctamente.

---

## Validaciones Necesarias Antes de Eliminar

### 1. INSTITUCIÓN
Antes de eliminar, validar:
- [ ] ¿Tiene usuarios asociados? (AppUser.InstitutionId)
- [ ] ¿Tiene entidades asociadas? (EntityProfile.InstitutionId)
- [ ] ¿Tiene carnets asociados? (Card.InstitutionId)
- [ ] ¿Tiene eventos asociados? (EventRecord.InstitutionId)
- [ ] ¿Tiene plantillas de carnet asociadas? (CardTemplate.InstitutionId)

**Mensaje de error sugerido:**
"No se puede eliminar la institución porque tiene [usuarios/entidades/carnets/eventos/plantillas] asociados. Elimine primero los elementos relacionados."

### 2. ENTIDAD
**✅ YA IMPLEMENTADO CORRECTAMENTE**
- ✅ Valida carnets asociados
- ✅ Valida eventos asociados

### 3. EVENTO
**✅ NO REQUIERE VALIDACIONES** - No tiene relaciones que impidan eliminación

### 4. CARNET
**✅ NO REQUIERE VALIDACIONES** - No tiene relaciones que impidan eliminación

### 5. USUARIO
Antes de eliminar, validar:
- [ ] ¿Es el usuario actual? (no puede eliminarse a sí mismo)
- [ ] ¿Es SuperAdmin? (protección adicional)
- [ ] ¿Tiene eventos creados? (opcional, para auditoría)
- [ ] ¿Tiene auditorías asociadas? (opcional, para auditoría)

**Mensaje de error sugerido:**
"No se puede eliminar el usuario porque [razón]. [Acción requerida]."

---

## Resumen de Tareas Pendientes

### CRÍTICO - Implementar Eliminaciones Faltantes:
1. ❌ **InstitutionsController.Delete** - Agregar acción con validaciones
2. ❌ **EventsController.Delete** - Agregar acción
3. ❌ **CardsController.Delete** - Agregar acción
4. ❌ **UsersController.Delete** - Agregar acción con validaciones

### IMPORTANTE - Agregar Funcionalidades Faltantes:
5. ❌ **UsersController.Edit** - Agregar acción Edit
6. ❌ **EventsController.Edit** - Agregar acción Edit
7. ❌ **EntityProfilesController.ToggleActive** - Agregar acción

### VALIDACIONES - Mejorar Servicios:
8. ❌ **InstitutionService.DeleteAsync** - Agregar validaciones de relaciones
9. ❌ **UsersController.Delete** - Agregar validaciones antes de eliminar

---

## Plan de Implementación

### Fase 1: Validaciones en Servicios
1. Mejorar `InstitutionService.DeleteAsync` con validaciones
2. Crear método de validación para usuarios antes de eliminar

### Fase 2: Acciones Delete en Controladores
1. Agregar `InstitutionsController.Delete` con validaciones
2. Agregar `EventsController.Delete`
3. Agregar `CardsController.Delete`
4. Agregar `UsersController.Delete` con validaciones

### Fase 3: Funcionalidades Adicionales
1. Agregar `UsersController.Edit`
2. Agregar `EventsController.Edit`
3. Agregar `EntityProfilesController.ToggleActive`

---

## Notas Importantes

1. **NO HAY ELIMINACIONES EN CASCADA** ✅ - El sistema está correctamente configurado
2. **DeleteBehavior.Restrict** está en todas las relaciones ✅
3. Las validaciones deben ser **explícitas y claras** para el usuario
4. Los mensajes de error deben indicar **qué elementos relacionados** impiden la eliminación
5. Todas las eliminaciones deben registrar **auditoría**

