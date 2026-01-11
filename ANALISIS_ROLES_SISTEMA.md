# Análisis de Roles del Sistema CarnetQR Platform

## Resumen Ejecutivo

El sistema implementa un modelo de **multi-tenancy** con 4 roles jerárquicos que controlan el acceso y las funcionalidades disponibles para cada usuario. Cada rol tiene permisos específicos basados en su nivel de responsabilidad dentro de la plataforma.

---

## 1. SuperAdmin (Administrador del Sistema)

### Descripción
Rol de **máximo nivel** con acceso completo a toda la plataforma. No está asociado a ninguna institución específica (`InstitutionId = null`), lo que le permite gestionar múltiples instituciones desde una sola cuenta.

### Características Clave
- **No tiene institución asignada**: `InstitutionId = null`
- **Acceso global**: Puede ver y gestionar todas las instituciones
- **Sin restricciones de tenant**: Bypassa el filtrado multi-tenant
- **Usuario por defecto**: `admin@qlservices.com` / `Admin@123456`

### Permisos y Funcionalidades

#### ✅ Gestión de Instituciones
- **Crear, editar, eliminar instituciones**
- Ver todas las instituciones del sistema
- Configurar datos de instituciones (nombre, logo, etc.)
- Asignar administradores a instituciones

#### ✅ Gestión de Usuarios
- Crear usuarios con roles: `InstitutionAdmin`, `Staff`, `AdministrativeOperator`
- **NO puede crear otros SuperAdmin** (solo se crean manualmente)
- Ver todos los usuarios del sistema
- Editar y desactivar usuarios de cualquier institución
- Asignar usuarios a cualquier institución

#### ✅ Gestión de Entidades
- Ver entidades de **todas las instituciones**
- Crear/editar/eliminar entidades de cualquier institución
- Debe seleccionar institución al crear entidades (no tiene una por defecto)

#### ✅ Gestión de Carnets
- Ver todos los carnets del sistema
- Generar carnets para cualquier entidad

#### ✅ Gestión de Eventos
- Ver eventos de todas las instituciones
- Crear eventos para cualquier institución
- **NO puede marcar atención** (solo Staff e InstitutionAdmin)

#### ✅ Dashboard y Estadísticas
- Ve estadísticas globales (todas las instituciones)
- Dashboard muestra contador de "Total Instituciones"
- Acceso a módulo de estadísticas

#### ❌ Restricciones
- **NO puede acceder a `/InstitutionConfig`** (solo para InstitutionAdmin)
- **NO puede marcar eventos como atendidos** (solo Staff/InstitutionAdmin)
- **NO puede configurar plantillas de carnet** por institución (usa módulo de Instituciones)

### Políticas de Autorización
- `[Authorize(Policy = "SuperAdminOnly")]` - Solo SuperAdmin
- `[Authorize(Policy = "InstitutionAdminOrAbove")]` - SuperAdmin + InstitutionAdmin
- `[Authorize(Policy = "StaffOrAbove")]` - SuperAdmin + InstitutionAdmin + Staff

---

## 2. InstitutionAdmin (Administrador de Institución)

### Descripción
Rol de **administración a nivel de institución**. Cada InstitutionAdmin está asociado a una institución específica y gestiona todos los aspectos operativos de esa institución.

### Características Clave
- **Tiene institución asignada**: `InstitutionId != null`
- **Acceso limitado a su institución**: Solo ve datos de su institución (multi-tenant)
- **Gestión operativa completa**: Control total sobre su institución

### Permisos y Funcionalidades

#### ✅ Gestión de Usuarios (de su institución)
- Crear usuarios con roles: `Staff`, `AdministrativeOperator`
- **NO puede crear InstitutionAdmin ni SuperAdmin**
- Ver, editar y desactivar usuarios de su institución
- Asignar usuarios solo a su institución

#### ✅ Gestión de Entidades
- Ver, crear, editar, eliminar entidades de **su institución**
- Cargar fotos de entidades
- Generar carnets para sus entidades

#### ✅ Gestión de Carnets
- Ver todos los carnets de su institución
- Generar, activar/desactivar carnets
- Configurar impresión de carnets

#### ✅ Gestión de Eventos
- Ver eventos de su institución
- Crear eventos para entidades de su institución
- **Puede marcar eventos como atendidos** (junto con Staff)

#### ✅ Configuración de Institución
- Acceso a `/InstitutionConfig`
- Configurar logo de institución
- Configurar campos visibles en carnets
- Configurar plantillas de carnet
- Habilitar/deshabilitar funcionalidades (ej: PhotoEnabled)

#### ✅ Dashboard y Estadísticas
- Ve estadísticas de **su institución**
- Dashboard muestra métricas de su institución
- Acceso a módulo de estadísticas

#### ❌ Restricciones
- **NO puede gestionar otras instituciones**
- **NO puede crear InstitutionAdmin ni SuperAdmin**
- **NO puede ver datos de otras instituciones** (filtrado multi-tenant)

### Políticas de Autorización
- `[Authorize(Policy = "InstitutionAdminOrAbove")]` - InstitutionAdmin + SuperAdmin
- `[Authorize(Policy = "StaffOrAbove")]` - InstitutionAdmin + Staff + SuperAdmin

---

## 3. Staff (Personal de Salud/Funcionario)

### Descripción
Rol operativo para **personal médico o de atención** que trabaja directamente con pacientes/entidades. Tiene acceso a funciones de atención pero no a configuración administrativa.

### Características Clave
- **Tiene institución asignada**: `InstitutionId != null`
- **Rol operativo**: Enfocado en atención y gestión de eventos
- **Sin acceso administrativo**: No gestiona usuarios ni configuración

### Permisos y Funcionalidades

#### ✅ Gestión de Entidades
- Ver, crear, editar entidades de **su institución**
- Cargar fotos de entidades
- Ver detalles de entidades

#### ✅ Gestión de Carnets
- Ver carnets de su institución
- Generar carnets para entidades de su institución
- Ver detalles de carnets

#### ✅ Gestión de Eventos (Funcionalidad Principal)
- Ver eventos de su institución
- Crear eventos para entidades de su institución
- **✅ Puede marcar eventos como atendidos** (funcionalidad clave)
- Cambiar estado de eventos (Scheduled → InProgress → Completed)

#### ❌ Restricciones
- **NO puede gestionar usuarios**
- **NO puede acceder a configuración de institución**
- **NO puede ver estadísticas** (solo InstitutionAdmin y SuperAdmin)
- **NO puede eliminar entidades o carnets** (solo lectura/creación)
- **NO puede ver datos de otras instituciones**

### Políticas de Autorización
- `[Authorize(Policy = "StaffOrAbove")]` - Staff + InstitutionAdmin + SuperAdmin
- Acceso básico a módulos operativos (sin restricciones explícitas en algunos controladores)

---

## 4. AdministrativeOperator (Operador Administrativo)

### Descripción
Rol de **soporte administrativo** con permisos limitados. Diseñado para personal que realiza tareas administrativas básicas pero no tiene autoridad médica.

### Características Clave
- **Tiene institución asignada**: `InstitutionId != null`
- **Permisos limitados**: Solo lectura y creación básica
- **Sin autoridad médica**: No puede marcar atención

### Permisos y Funcionalidades

#### ✅ Gestión de Entidades (Limitado)
- Ver entidades de **su institución**
- Crear/editar entidades (probablemente)
- Cargar fotos de entidades

#### ✅ Gestión de Carnets (Limitado)
- Ver carnets de su institución
- Generar carnets para entidades

#### ✅ Gestión de Eventos (Solo Lectura/Creación)
- Ver eventos de su institución
- Crear eventos para entidades
- **❌ NO puede marcar eventos como atendidos** (restricción explícita)

#### ❌ Restricciones Principales
- **NO puede marcar atención en eventos** (solo Staff e InstitutionAdmin pueden)
- **NO puede gestionar usuarios**
- **NO puede acceder a configuración**
- **NO puede ver estadísticas**
- **NO puede ver datos de otras instituciones**

### Código de Restricción
```csharp
// EventsController.cs - UpdateStatus
// AdministrativeOperator NO puede marcar atención según especificación
if (status != EventStatus.Scheduled && 
    !User.IsInRole(Roles.Staff) && 
    !User.IsInRole(Roles.InstitutionAdmin) && 
    !User.IsInRole(Roles.SuperAdmin))
{
    return Json(new { success = false, message = "No tiene permisos para marcar atención..." });
}
```

---

## Matriz de Permisos Comparativa

| Funcionalidad | SuperAdmin | InstitutionAdmin | Staff | AdministrativeOperator |
|--------------|------------|------------------|-------|----------------------|
| **Gestionar Instituciones** | ✅ | ❌ | ❌ | ❌ |
| **Gestionar Usuarios** | ✅ (todos) | ✅ (solo Staff/AdminOp) | ❌ | ❌ |
| **Configuración Institución** | ❌ | ✅ | ❌ | ❌ |
| **Ver Estadísticas** | ✅ (global) | ✅ (su inst.) | ❌ | ❌ |
| **Gestionar Entidades** | ✅ (todas) | ✅ (su inst.) | ✅ (su inst.) | ✅ (su inst.) |
| **Gestionar Carnets** | ✅ (todos) | ✅ (su inst.) | ✅ (su inst.) | ✅ (su inst.) |
| **Crear Eventos** | ✅ (todas) | ✅ (su inst.) | ✅ (su inst.) | ✅ (su inst.) |
| **Marcar Atención** | ❌ | ✅ | ✅ | ❌ |
| **Ver Otras Instituciones** | ✅ | ❌ | ❌ | ❌ |

---

## Jerarquía de Roles

```
SuperAdmin (Nivel 4 - Máximo)
    │
    ├── InstitutionAdmin (Nivel 3 - Administración Institución)
    │       │
    │       ├── Staff (Nivel 2 - Operativo Médico)
    │       │
    │       └── AdministrativeOperator (Nivel 1 - Soporte)
```

### Reglas de Creación de Usuarios

1. **SuperAdmin**:
   - Solo puede crear: `InstitutionAdmin`, `Staff`, `AdministrativeOperator`
   - NO puede crear otros SuperAdmin
   - NO tiene institución asignada

2. **InstitutionAdmin**:
   - Solo puede crear: `Staff`, `AdministrativeOperator`
   - NO puede crear `InstitutionAdmin` ni `SuperAdmin`
   - Solo puede crear usuarios para su institución

3. **Staff y AdministrativeOperator**:
   - NO pueden crear usuarios

---

## Multi-Tenancy y Filtrado

### SuperAdmin
- **Sin filtrado**: Ve todos los datos de todas las instituciones
- `TenantProvider.GetCurrentTenantId()` retorna `null`
- Bypassa filtros de `InstitutionId`

### InstitutionAdmin, Staff, AdministrativeOperator
- **Filtrado estricto**: Solo ven datos de su institución
- `TenantProvider.GetCurrentTenantId()` retorna su `InstitutionId`
- Todos los queries aplican filtro `WHERE InstitutionId = @tenantId`
- No pueden cambiar su `InstitutionId` (protección multi-tenant)

---

## Políticas de Autorización Definidas

```csharp
// DependencyInjection.cs
options.AddPolicy("SuperAdminOnly", 
    policy => policy.RequireRole(Roles.SuperAdmin));

options.AddPolicy("InstitutionAdminOrAbove", 
    policy => policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin));

options.AddPolicy("StaffOrAbove", 
    policy => policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Staff));
```

---

## Casos de Uso por Rol

### SuperAdmin
- **Uso típico**: Administrador de la plataforma, soporte técnico, gestión de múltiples clientes
- **Escenario**: "Necesito crear una nueva institución y asignarle un administrador"

### InstitutionAdmin
- **Uso típico**: Director de clínica, gerente de institución de salud
- **Escenario**: "Necesito configurar los campos del carnet y crear usuarios para mi equipo"

### Staff
- **Uso típico**: Médico, enfermero, personal de atención
- **Escenario**: "Necesito marcar que atendí a un paciente y ver sus eventos programados"

### AdministrativeOperator
- **Uso típico**: Recepcionista, personal administrativo, secretaría
- **Escenario**: "Necesito crear un evento para un paciente y generar su carnet, pero no puedo marcar atención"

---

## Notas de Seguridad

1. **Multi-tenant estricto**: Los roles no-SuperAdmin están completamente aislados por institución
2. **Validación de InstitutionId**: No se puede cambiar el `InstitutionId` de una entidad durante actualizaciones
3. **Protección de roles**: InstitutionAdmin no puede escalar privilegios creando otros InstitutionAdmin
4. **SuperAdmin sin institución**: El SuperAdmin no puede tener `InstitutionId` asignado (validación explícita)

---

## Conclusión

El sistema implementa un modelo de roles jerárquico y multi-tenant que separa claramente las responsabilidades:
- **SuperAdmin**: Gestión global y multi-institucional
- **InstitutionAdmin**: Administración completa de una institución
- **Staff**: Operaciones médicas y atención
- **AdministrativeOperator**: Soporte administrativo sin autoridad médica

Cada rol tiene permisos específicos diseñados para su función, con restricciones de seguridad que previenen escalación de privilegios y garantizan el aislamiento de datos entre instituciones.

