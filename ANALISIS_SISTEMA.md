# Análisis del Sistema - CarnetQR Platform

## 📋 Resumen Ejecutivo

**CarnetQR Platform** es una aplicación web ASP.NET Core 8.0 diseñada para la gestión de carnets con código QR para clínicas y hospitales. El sistema permite a las instituciones médicas emitir carnets a pacientes con códigos QR que permiten consultar información de citas médicas sin requerir autenticación.

---

## 🏗️ Arquitectura del Sistema

### Arquitectura en Capas (Layered Architecture)

El sistema sigue una arquitectura limpia organizada en 4 capas principales:

1. **CarnetQRPlatform.Domain** - Capa de Dominio
   - Entidades del dominio
   - Enumeraciones y constantes
   - Interfaces básicas

2. **CarnetQRPlatform.Application** - Capa de Aplicación
   - Interfaces de servicios
   - Lógica de negocio (contratos)

3. **CarnetQRPlatform.Infrastructure** - Capa de Infraestructura
   - Acceso a datos (Entity Framework Core)
   - Implementación de servicios
   - Middleware personalizado
   - Migraciones de base de datos

4. **CarnetQRPlatform.Web** - Capa de Presentación
   - Controladores MVC
   - Vistas Razor
   - Servicios web específicos

---

## 🗄️ Modelo de Datos

### Entidades Principales

#### 1. **Institution** (Institución)
- Representa las clínicas/hospitales que usan el sistema
- **Propiedades clave:**
  - `Name`, `Description`, `Email`, `Phone`, `Address`
  - `CardPrefix`: Prefijo para numeración de carnets (ej: "HEMO")
  - `InstitutionType`: Tipo (Clínica/Hospital)
  - `PhotoEnabled`: Configuración para habilitar fotos en carnets
  - `VisibleFields`: Lista de hasta 6 campos visibles en el carnet
  - `QrPublicDisplayMode`: Modo de visualización (Número de carnet o Nombre)
  - `PatientDataVisibilityConfig`: Configuración global de visibilidad de datos
  - `LogoPath`: Ruta al logo institucional

#### 2. **EntityProfile** (Perfil de Entidad/Paciente)
- Representa a los pacientes/beneficiarios
- **Propiedades clave:**
  - `IdentificationNumber`, `FirstName`, `LastName`
  - `Email`, `Phone`, `DateOfBirth`
  - `PhotoPath`: Ruta a la foto del paciente
  - `CustomFields`: Diccionario JSON para campos personalizados
  - `PatientDataVisibilityOverride`: Configuración específica por paciente

#### 3. **Card** (Carnet)
- Representa los carnets físicos/digitales emitidos
- **Propiedades clave:**
  - `CardNumber`: Número único con prefijo (ej: "HEMO-000001")
  - `QrToken`: Token único y seguro para acceso QR
  - `IssuedAt`, `ExpiresAt`
  - `IsActive`: Estado activo/inactivo

#### 4. **EventRecord** (Registro de Cita/Evento)
- Representa las citas médicas programadas
- **Propiedades clave:**
  - `ScheduledAt`: Fecha y hora programada
  - `Status`: Estado (Scheduled, Completed, NotCompleted)
  - `Notes`: Observaciones opcionales
  - `CompletedAt`, `CompletedBy`: Información de finalización

#### 5. **CardTemplate** (Plantilla de Carnet)
- Plantillas personalizables para diseño de carnets
- **Propiedades clave:**
  - `Name`, `IsDefault`
  - `TemplateHtml`: HTML de la plantilla
  - `TemplateConfig`: Configuración JSON de la plantilla

#### 6. **AppUser** (Usuario)
- Extiende `IdentityUser` de ASP.NET Core Identity
- **Propiedades clave:**
  - `FirstName`, `LastName`
  - `InstitutionId`: Relación con institución (null para SuperAdmin)
  - `IsActive`, `LastLoginAt`

#### 7. **AuditLog** (Log de Auditoría)
- Registro de acciones del sistema
- Rastrea: Action, Entity, EntityId, UserId, Timestamp, Metadata

### Relaciones Entre Entidades

```
Institution (1) ────< (N) EntityProfile
Institution (1) ────< (N) Card
Institution (1) ────< (N) EventRecord
Institution (1) ────< (N) CardTemplate
Institution (1) ────< (N) AppUser
EntityProfile (1) ────< (N) Card
EntityProfile (1) ────< (N) EventRecord
```

---

## 🔐 Sistema de Seguridad y Multi-Tenant

### Multi-Tenancy (Multi-Institucional)

El sistema implementa un modelo multi-tenant estricto:

1. **Aislamiento por Institución:**
   - Cada entidad (Card, EntityProfile, EventRecord) tiene `InstitutionId`
   - Las consultas se filtran automáticamente por tenant
   - Validación estricta en `SaveChangesAsync` para prevenir cambios de `InstitutionId`

2. **TenantProvider:**
   - Obtiene el `InstitutionId` del usuario autenticado desde claims
   - SuperAdmin tiene acceso a todas las instituciones
   - Middleware `TenantMiddleware` establece el contexto del tenant

3. **Protecciones Implementadas:**
   - Validación en tiempo de guardado
   - Filtrado automático en servicios
   - Restricción de acceso basada en roles

### Sistema de Roles y Permisos

#### Roles Definidos:
1. **SuperAdmin**
   - Acceso a todas las instituciones
   - Puede crear instituciones
   - No pertenece a ninguna institución específica

2. **InstitutionAdmin**
   - Administrador de una institución
   - Gestiona usuarios internos
   - Configuración institucional
   - Consulta estadísticas

3. **Staff** (Funcionario de Salud)
   - Crea y edita pacientes
   - Registra citas médicas
   - Marca atenciones como realizadas/no realizadas

4. **AdministrativeOperator** (Operador Administrativo)
   - Crea pacientes
   - Registra citas
   - No puede marcar atenciones

### Autenticación y Autorización

- **ASP.NET Core Identity** para gestión de usuarios
- **Configuración de cookies:**
  - Tiempo de expiración: 8 horas
  - Sliding expiration habilitado
  - Bloqueo después de 5 intentos fallidos (15 minutos)

### Seguridad Adicional

1. **Rate Limiting:**
   - Endpoints públicos: 10 requests/minuto
   - Endpoints autenticados: 30 requests/minuto
   - Implementado en `RateLimitMiddleware`

2. **Security Headers:**
   - X-Content-Type-Options: nosniff
   - X-Frame-Options: DENY
   - X-XSS-Protection
   - Content Security Policy (CSP)
   - Referrer-Policy

3. **Logging:**
   - Serilog para logging estructurado
   - Logs en consola y archivo (rolling daily)
   - Retención de 7 días

---

## 🔄 Flujos Principales del Sistema

### 1. Flujo de Acceso Público QR

```
Usuario escanea QR
    ↓
GET /q/{token} (AllowAnonymous)
    ↓
CardService.GetByQrTokenAsync(token)
    ↓
Validar card activo
    ↓
Obtener citas (próximas e histórico)
    ↓
Aplicar configuración de visualización institucional
    ↓
Mostrar vista pública con información
```

**Características:**
- Sin autenticación requerida
- Solo lectura (no se permite edición)
- Información según configuración institucional
- Rate limiting aplicado (10 req/min)

### 2. Flujo de Creación de Carnet

```
Usuario autenticado (Staff/Admin)
    ↓
POST /Cards/Create (EntityProfileId)
    ↓
Validar tenant context
    ↓
Obtener EntityProfile (con validación tenant)
    ↓
Generar número de carnet (Prefijo + Consecutivo)
    ↓
Generar QR Token seguro (32 caracteres)
    ↓
Crear Card con InstitutionId del EntityProfile
    ↓
Guardar en base de datos
    ↓
Retornar Card creado
```

**Validaciones:**
- EntityProfile debe pertenecer al tenant del usuario
- Número de carnet único por institución
- QR Token único globalmente

### 3. Flujo de Gestión de Citas

```
Usuario autenticado (Staff)
    ↓
Crear/Editar EventRecord
    ↓
Validar EntityProfile pertenece a tenant
    ↓
ScheduledAt debe ser válida
    ↓
Al marcar como completada:
    - Validar que ScheduledAt ya pasó
    - Registrar CompletedBy y CompletedAt
    ↓
Guardar cambios
```

---

## 🗃️ Base de Datos

### Tecnología
- **PostgreSQL** como base de datos
- **Entity Framework Core** como ORM
- **Npgsql** como proveedor de PostgreSQL

### Migraciones
El sistema incluye migraciones para:
- Creación inicial de esquema
- Modificaciones de estructura
- Índices y constraints

### Configuración JSON
Varios campos utilizan almacenamiento JSON:
- `VisibleFields` (List<string>)
- `CustomFields` (Dictionary<string, object>)
- `PatientDataVisibilityConfig` (Dictionary<string, bool>)
- `TemplateConfig` (Dictionary<string, object>)
- `Metadata` en AuditLog

---

## 🎨 Frontend

### Tecnología
- **ASP.NET Core MVC** con vistas Razor
- **Bootstrap** para estilos
- **jQuery** y **DataTables** para interactividad
- **Font Awesome** para iconos

### Estructura de Vistas
- Layout principal (`_Layout.cshtml`)
- Vistas por controlador:
  - Account (Login, etc.)
  - Home (Dashboard)
  - Cards (Gestión de carnets)
  - EntityProfiles (Gestión de pacientes)
  - Events (Gestión de citas)
  - Institutions (Gestión institucional)
  - Qr (Vista pública de QR)
  - Statistics (Estadísticas)
  - Users (Gestión de usuarios)

---

## 🎮 Controladores y Endpoints

### Controladores Implementados

#### 1. **AccountController**
- **Responsabilidades:** Autenticación y gestión de sesión
- **Endpoints:**
  - `GET/POST /Account/Login` - Inicio de sesión
  - `POST /Account/Logout` - Cerrar sesión
  - `GET /Account/AccessDenied` - Acceso denegado
  - `GET/POST /Account/ChangePassword` - Cambio de contraseña
- **Autorización:** `[AllowAnonymous]` para Login, `[Authorize]` para otros
- **Características:**
  - Establece claim `InstitutionId` durante login
  - Bloqueo de cuenta después de 5 intentos fallidos
  - Registro de intentos de login en logs

#### 2. **HomeController**
- **Responsabilidades:** Dashboard principal
- **Endpoints:**
  - `GET /` - Página principal (Dashboard)
  - `GET /Home/Privacy` - Política de privacidad
  - `GET /Home/Error` - Página de error
- **Autorización:** `[Authorize]` (requiere autenticación)

#### 3. **InstitutionsController**
- **Responsabilidades:** Gestión de instituciones
- **Endpoints:**
  - `GET /Institutions` - Lista de instituciones
  - `GET /Institutions/Create` - Formulario de creación
  - `POST /Institutions/Create` - Crear institución
  - `GET /Institutions/Edit/{id}` - Formulario de edición
  - `POST /Institutions/Edit/{id}` - Actualizar institución
  - `GET /Institutions/Details/{id}` - Detalles de institución
  - `POST /Institutions/Delete/{id}` - Eliminar institución
- **Autorización:** `[Authorize(Policy = "SuperAdminOnly")]`
- **Características:** Solo SuperAdmin puede gestionar instituciones

#### 4. **EntityProfilesController**
- **Responsabilidades:** Gestión de pacientes/beneficiarios
- **Endpoints:**
  - `GET /EntityProfiles` - Lista de pacientes
  - `GET /EntityProfiles/Create` - Formulario de creación
  - `POST /EntityProfiles/Create` - Crear paciente
  - `GET /EntityProfiles/Edit/{id}` - Formulario de edición
  - `POST /EntityProfiles/Edit/{id}` - Actualizar paciente
  - `GET /EntityProfiles/Details/{id}` - Detalles de paciente
  - `POST /EntityProfiles/Delete/{id}` - Eliminar paciente
- **Autorización:** `[Authorize]` (Staff, Admin, SuperAdmin)
- **Características:**
  - Filtrado automático por tenant
  - Gestión de campos personalizados
  - Upload de fotos de pacientes

#### 5. **CardsController**
- **Responsabilidades:** Gestión de carnets
- **Endpoints:**
  - `GET /Cards` - Lista de carnets
  - `POST /Cards/Create` - Crear carnet para un paciente
  - `GET /Cards/Details/{id}` - Detalles de carnet
  - `POST /Cards/ToggleActive/{id}` - Activar/desactivar carnet
  - `POST /Cards/Delete/{id}` - Eliminar carnet
  - `GET /Cards/Print/{id}` - Vista de impresión
- **Autorización:** `[Authorize]` (Staff, Admin, SuperAdmin)
- **Características:**
  - Generación automática de número de carnet
  - Generación de token QR único
  - Validación estricta de tenant

#### 6. **EventsController**
- **Responsabilidades:** Gestión de citas médicas
- **Endpoints:**
  - `GET /Events` - Lista de citas
  - `GET /Events/Create` - Formulario de creación
  - `POST /Events/Create` - Crear cita
  - `GET /Events/Edit/{id}` - Formulario de edición
  - `POST /Events/Edit/{id}` - Actualizar cita
  - `GET /Events/Details/{id}` - Detalles de cita
  - `POST /Events/MarkCompleted/{id}` - Marcar como completada
  - `POST /Events/MarkNotCompleted/{id}` - Marcar como no completada
  - `POST /Events/Delete/{id}` - Eliminar cita
- **Autorización:** `[Authorize]` (Staff, Admin, SuperAdmin)
- **Características:**
  - Validación de fechas (no se puede completar antes de la fecha programada)
  - Registro de usuario que completó la cita
  - Estados: Scheduled, Completed, NotCompleted

#### 7. **QrController**
- **Responsabilidades:** Visualización pública de información QR
- **Endpoints:**
  - `GET /q/{token}` - Visualizar información del carnet (público)
- **Autorización:** `[AllowAnonymous]` (público, sin autenticación)
- **Características:**
  - Acceso sin autenticación
  - Rate limiting aplicado (10 req/min)
  - Respeta configuración de visibilidad de datos
  - Muestra citas futuras e historial
  - Genera código QR para mostrar en la vista

#### 8. **UsersController**
- **Responsabilidades:** Gestión de usuarios del sistema
- **Endpoints:**
  - `GET /Users` - Lista de usuarios
  - `GET /Users/Create` - Formulario de creación
  - `POST /Users/Create` - Crear usuario
  - `GET /Users/Edit/{id}` - Formulario de edición
  - `POST /Users/Edit/{id}` - Actualizar usuario
  - `GET /Users/Details/{id}` - Detalles de usuario
  - `POST /Users/ToggleActive/{id}` - Activar/desactivar usuario
  - `POST /Users/Delete/{id}` - Eliminar usuario
- **Autorización:** `[Authorize(Policy = "InstitutionAdminOrAbove")]`
- **Características:**
  - Solo InstitutionAdmin y SuperAdmin pueden gestionar usuarios
  - Asignación de roles
  - Vinculación con instituciones

#### 9. **StatisticsController**
- **Responsabilidades:** Estadísticas y reportes
- **Endpoints:**
  - `GET /Statistics` - Dashboard de estadísticas
- **Autorización:** `[Authorize(Policy = "InstitutionAdminOrAbove")]`
- **Características:**
  - Estadísticas de citas (programadas, completadas, no completadas)
  - Tasas de asistencia y completitud
  - Tendencias por período (semana, mes, año)
  - Estadísticas de carnets emitidos
  - Estadísticas de pacientes

#### 10. **InstitutionConfigController**
- **Responsabilidades:** Configuración institucional
- **Endpoints:**
  - `GET /InstitutionConfig` - Configuración actual
  - `POST /InstitutionConfig` - Actualizar configuración
- **Autorización:** `[Authorize(Policy = "InstitutionAdminOrAbove")]`
- **Características:**
  - Configuración de visibilidad de datos
  - Configuración de campos visibles en carnet
  - Configuración de modo de visualización QR
  - Upload de logo institucional

#### 11. **CarnetController**
- **Responsabilidades:** Visualización e impresión de carnets
- **Endpoints:**
  - `GET /Carnet/Print/{id}` - Vista de impresión de carnet
- **Autorización:** `[Authorize]`
- **Características:**
  - Generación de vista imprimible
  - Incluye código QR
  - Respeta configuración institucional

#### 12. **TestController** (Solo Desarrollo)
- **Responsabilidades:** Endpoints de prueba
- **Endpoints:**
  - `GET /Test/CheckUsers` - Verificar usuarios creados
- **Autorización:** `[AllowAnonymous]` (solo desarrollo)
- **Características:**
  - Muestra usuarios, roles e instituciones en JSON
  - Útil para debugging

---

## 🔧 Servicios y Lógica de Negocio

### Servicios Implementados

1. **CardService**
   - CRUD de carnets
   - Generación de números de carnet
   - Generación de tokens QR seguros
   - Filtrado multi-tenant

2. **EntityProfileService**
   - CRUD de pacientes
   - Gestión de campos personalizados
   - Validación de visibilidad de datos

3. **EventService**
   - CRUD de citas
   - Validación de fechas
   - Cambio de estado (solo después de fecha programada)

4. **InstitutionService**
   - CRUD de instituciones (solo SuperAdmin)
   - Configuración institucional
   - Gestión de logos y plantillas

5. **CardTemplateService**
   - Gestión de plantillas de carnets
   - Configuración de campos visibles

6. **AuditService**
   - Registro de acciones
   - Trazabilidad de cambios

7. **TenantProvider**
   - Obtención de contexto de tenant
   - Detección de SuperAdmin

---

## 📊 Funcionalidades Principales

### Para Administradores de Institución:
- ✅ Gestión de usuarios internos
- ✅ Configuración de visibilidad de datos (global y por paciente)
- ✅ Configuración de carnets (campos visibles, foto, prefijo)
- ✅ Importación de logo institucional
- ✅ Consulta de estadísticas

### Para Funcionarios de Salud:
- ✅ Crear y editar pacientes
- ✅ Registrar citas médicas
- ✅ Marcar atenciones como realizadas/no realizadas
- ✅ Consultar información de pacientes

### Para Pacientes/Cuidadores (QR Público):
- ✅ Visualizar información del paciente (según configuración)
- ✅ Ver citas futuras programadas
- ✅ Ver historial completo de citas
- ✅ Información institucional (logo, teléfono, dirección, indicaciones)

---

## 🚀 Puntos Fuertes del Sistema

1. **Arquitectura limpia y bien organizada**
   - Separación de responsabilidades
   - Código mantenible y escalable

2. **Seguridad robusta**
   - Multi-tenancy estricto
   - Validaciones en múltiples capas
   - Rate limiting
   - Security headers

3. **Flexibilidad en configuración**
   - Campos personalizables
   - Configuración por institución
   - Configuración por paciente (override)

4. **Trazabilidad completa**
   - Audit logs de todas las acciones
   - Timestamps automáticos
   - Registro de usuario que realizó acción

5. **Experiencia de usuario**
   - Interfaz intuitiva con Bootstrap
   - Acceso público sin autenticación para QR
   - Visualización responsive

---

## ⚠️ Áreas de Mejora Potencial

1. **Documentación**
   - Falta documentación API más detallada
   - Comentarios en código podrían ser más extensos

2. **Testing**
   - No se observan tests unitarios o de integración

3. **Configuración**
   - Algunas configuraciones están hardcodeadas (ej: rate limits)
   - Podrían moverse a `appsettings.json`

4. **Validación**
   - Validaciones adicionales en modelos (Data Annotations)
   - Validaciones más robustas de negocio

5. **Performance**
   - Considerar caché para consultas frecuentes
   - Optimización de queries (incluye automáticos)

6. **Internacionalización**
   - Textos hardcodeados en español
   - No hay soporte multi-idioma

7. **Upload de Archivos**
   - No se observa gestión explícita de upload de logos/fotos
   - Validación de tipos de archivo y tamaños

---

## 📝 Conclusión

CarnetQR Platform es un sistema bien estructurado que cumple con los requisitos funcionales establecidos en la especificación. La arquitectura en capas, el sistema de multi-tenancy robusto y las medidas de seguridad implementadas demuestran un diseño profesional.

El sistema está listo para uso en producción, aunque se recomendarían mejoras en testing, documentación y algunas optimizaciones antes de un despliegue a gran escala.

---

## 📌 Información Técnica Adicional

### Stack Tecnológico

- **Framework:** .NET 8.0
- **ORM:** Entity Framework Core 8.0.11
- **Base de Datos:** PostgreSQL (Npgsql)
- **Autenticación:** ASP.NET Core Identity 8.0.11
- **Logging:** Serilog 10.0.0 (Console + File sinks)
- **QR Code:** QRCoder 1.7.0
- **Frontend:** 
  - Bootstrap
  - jQuery
  - DataTables 2.3.6
  - Font Awesome
- **Patrón:** Repository/Service Pattern
- **Arquitectura:** Clean Architecture / Layered Architecture

### Dependencias Principales

```xml
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.11)
- Microsoft.EntityFrameworkCore.Design (8.0.11)
- QRCoder (1.7.0)
- Serilog.AspNetCore (10.0.0)
- AspNetCore.HealthChecks.UI.Client (9.0.0)
- datatables.net (2.3.6)
```

### Configuración de Base de Datos

- **Proveedor:** Npgsql (PostgreSQL)
- **Migraciones:** Entity Framework Core Migrations
- **Configuración JSON:** 
  - Campos complejos almacenados como JSON (VisibleFields, CustomFields, PatientDataVisibilityConfig, TemplateConfig, Metadata)
  - Conversión automática mediante `JsonSerializer`
  - ValueComparers personalizados para comparación de colecciones

### Middleware Pipeline

El orden del middleware en `Program.cs` es crítico para la seguridad:

1. **Exception Handler** (solo producción)
2. **HTTPS Redirection**
3. **Static Files**
4. **Serilog Request Logging**
5. **Rate Limit Middleware** (temprano en el pipeline)
6. **Routing**
7. **Security Headers** (CSP, X-Frame-Options, etc.)
8. **Authentication**
9. **Tenant Middleware** (después de Authentication, antes de Authorization)
10. **Authorization**
11. **MVC Controllers**

### Generación de Tokens QR

- **Algoritmo:** `RandomNumberGenerator` (criptográficamente seguro)
- **Longitud:** 32 caracteres
- **Formato:** Base64 URL-safe (reemplaza `+`, `/`, `=` por `-`, `_`, y elimina padding)
- **Unicidad:** Validado mediante índice único en base de datos
- **Ejemplo:** `aBcD1234eFgH5678iJkL9012mNoP3456`

### Rate Limiting

- **Implementación:** Middleware personalizado con `ConcurrentDictionary` en memoria
- **Límites:**
  - Endpoints públicos (QR): 10 requests/minuto por IP
  - Endpoints autenticados: 30 requests/minuto por IP
  - Usuarios autenticados en endpoints no-QR: sin límite
- **Ventana de tiempo:** 1 minuto (rolling window)
- **Headers de respuesta:**
  - `X-RateLimit-Limit`: Límite máximo
  - `X-RateLimit-Remaining`: Solicitudes restantes
  - `X-RateLimit-Reset`: Timestamp de reset
  - `Retry-After`: Segundos hasta poder reintentar (cuando se excede)
- **Limpieza:** Automática cada 30 segundos para entradas expiradas

### Seguridad Multi-Tenant

**Validaciones en múltiples capas:**

1. **Capa de Servicio:**
   - `ApplyTenantFilter()` extension method filtra automáticamente por `InstitutionId`
   - Validación explícita en métodos de creación

2. **Capa de Base de Datos:**
   - `SaveChangesAsync()` valida que `InstitutionId` no cambie en updates
   - Lanza `InvalidOperationException` si se detecta intento de cambio
   - Restaura automáticamente el `InstitutionId` original

3. **Capa de Middleware:**
   - `TenantMiddleware` establece contexto de tenant en `HttpContext.Items`
   - `TenantProvider` obtiene tenant desde claims del usuario

4. **Capa de Controlador:**
   - Políticas de autorización basadas en roles
   - Validación de pertenencia a institución

### Configuración de Logging

- **Sinks:** Console + File (rolling daily)
- **Retención:** 7 días
- **Niveles:**
  - Default: Information
  - Microsoft: Warning
  - System: Warning
- **Ubicación:** `logs/log-{date}.txt`

### Configuración de Cookies

- **Expiración:** 8 horas
- **Sliding Expiration:** Habilitado
- **Lockout:** 
  - 5 intentos fallidos
  - Bloqueo por 15 minutos
- **Rutas:**
  - Login: `/Account/Login`
  - Logout: `/Account/Logout`
  - Access Denied: `/Account/AccessDenied`

### Endpoints Públicos

- **`GET /q/{token}`** - Visualización pública de información del carnet
  - Sin autenticación requerida (`[AllowAnonymous]`)
  - Rate limiting aplicado (10 req/min)
  - Muestra información según configuración institucional
  - Respeta `PatientDataVisibilityConfig` y `PatientDataVisibilityOverride`

### Índices de Base de Datos

**Índices únicos:**
- `Institutions.CardPrefix` (único)
- `Cards.CardNumber` (único)
- `Cards.QrToken` (único)

**Índices para performance:**
- `EntityProfiles.InstitutionId`
- `EntityProfiles.(InstitutionId, IdentificationNumber)`
- `Cards.InstitutionId`
- `Cards.EntityProfileId`
- `EventRecords.InstitutionId`
- `EventRecords.EntityProfileId`
- `EventRecords.ScheduledAt`
- `AuditLogs.InstitutionId`
- `AuditLogs.Timestamp`
- `AuditLogs.(Entity, EntityId)`

### Extensiones de DbContext

Métodos de extensión para filtrado multi-tenant:
- `ApplyTenantFilter<T>()` - Filtro genérico para entidades `ITenantEntity`
- `GetTenantEntityProfiles()` - EntityProfiles del tenant
- `GetTenantCards()` - Cards del tenant
- `GetTenantCardTemplates()` - Templates del tenant
- `GetTenantEventRecords()` - EventRecords del tenant
- `GetTenantAuditLogs()` - AuditLogs del tenant

---

## 🔍 Análisis Profundo de Componentes

### Inicialización de Base de Datos (DbInitializer)

**Proceso de inicialización:**
1. **Migraciones:** Ejecuta automáticamente todas las migraciones pendientes
2. **Roles:** Crea roles del sistema (SuperAdmin, InstitutionAdmin, Staff, AdministrativeOperator)
3. **SuperAdmin:** Crea usuario SuperAdmin por defecto:
   - Email: `admin@qlservices.com`
   - Password: `Admin@123456`
   - Sin institución asignada (`InstitutionId = null`)
   - Sin bloqueo de cuenta
4. **Demo Institution:** Crea institución de demostración:
   - Nombre: "Empresa Demo"
   - Prefijo: "DEMO"
   - Tipo: Clínica
   - Usuario admin: `admin@demo.com` / `Admin@123456`
   - Rol: InstitutionAdmin

**Características:**
- Validación de existencia antes de crear
- Logging detallado de cada operación
- Manejo de errores con re-throw para debugging
- No sobrescribe usuarios existentes

### Flujo de Autenticación

**Proceso de Login:**
1. Validación de ModelState (email, password)
2. Búsqueda de usuario por email
3. Validación de usuario activo
4. Intentos de login con bloqueo (5 intentos = 15 min bloqueo)
5. **Establecimiento de Claims:**
   - Si usuario tiene `InstitutionId`, se agrega claim `InstitutionId`
   - Claim se actualiza si cambió la institución
   - `RefreshSignInAsync` para incluir claim en sesión actual
6. Actualización de `LastLoginAt`
7. Redirección según rol (todos van a Home/Index)

**Estados de Login:**
- `Succeeded`: Login exitoso
- `IsLockedOut`: Cuenta bloqueada (página Lockout)
- `RequiresTwoFactor`: Requiere 2FA (no implementado)
- `IsNotAllowed`: Cuenta no permitida (email no confirmado)
- `Failed`: Credenciales inválidas

### Validaciones de Negocio

#### EntityProfile (Pacientes)
- **Creación:**
  - Validación de tenant context (excepto SuperAdmin)
  - SuperAdmin debe proporcionar `InstitutionId` explícitamente
  - Conversión de `DateOfBirth` a UTC (requisito PostgreSQL)
  - `InstitutionId` se fuerza desde tenant (no se acepta del request)
  
- **Actualización:**
  - Validación de existencia con filtro tenant
  - `InstitutionId` no puede cambiar (validación adicional)
  - Solo se actualizan campos permitidos
  - `DateOfBirth` se convierte a UTC
  
- **Eliminación:**
  - Validación de no tener Cards asociados
  - Validación de no tener EventRecords asociados
  - Mensajes de error descriptivos

#### Card (Carnets)
- **Creación:**
  - Validación de tenant context
  - Validación de EntityProfile pertenece al tenant
  - Generación de número único: `{Prefix}{6 dígitos consecutivos}` (ej: "DEMO000001")
  - Generación de QR Token seguro (32 caracteres, Base64 URL-safe)
  - Validación de unicidad de `CardNumber` (por institución)
  - Validación de unicidad global de `QrToken`
  
- **Activación/Desactivación:**
  - Toggle de `IsActive`
  - Auditoría de cambios
  - Registro de acción con metadata

#### EventRecord (Citas)
- **Creación:**
  - Validación de tenant context
  - Validación de EntityProfile pertenece al tenant
  - Estado inicial: `Scheduled`
  - Conversión de `ScheduledAt` a UTC
  
- **Cambio de Estado:**
  - No se puede completar antes de `ScheduledAt`
  - Validación: `ScheduledAt > DateTime.UtcNow` → error
  - Actualización de `CompletedAt` cuando cambia estado
  - Estados posibles: `Scheduled`, `Completed`, `NotCompleted`
  
- **Consultas:**
  - `GetUpcomingAsync`: Citas futuras con estado `Scheduled`
  - `GetByEntityProfileAsync`: Todas las citas de un paciente (filtrado por tenant)

#### Institution (Institución)
- **Creación:**
  - Validación de unicidad de `CardPrefix` (índice único)
  - Manejo de excepción `DbUpdateException` con código PostgreSQL 23505 (unique violation)
  - Mensajes de error descriptivos
  
- **Validaciones:**
  - `CardPrefix`: Máximo 10 caracteres, único globalmente
  - `Name`: Máximo 200 caracteres, requerido
  - Índice en `Name` para búsquedas rápidas

#### CardTemplate (Plantillas)
- **Creación:**
  - Máximo 6 campos visibles (`VisibleFields.Count <= 6`)
  - Si es primera plantilla o `IsDefault = true`, se marca como default
  - Si se marca como default, desmarca otros templates del tenant
  
- **Actualización:**
  - Validación de máximo 6 campos visibles
  - Lógica compleja para manejo de template default:
    - Si se marca como default → desmarca otros
    - Si se desmarca y es el único → no permite desmarcar
    - Si se desmarca y hay otros → marca el primero como default
  
- **Eliminación:**
  - No permite eliminar si es el único template
  - Si era default, marca otro como default automáticamente

### Servicio de Código QR (QrCodeService)

**Funcionalidades:**
- `GenerateQrCodeBase64(string url, int size)`: Generación básica
- `GenerateQrCodeBase64(string url, int size, string darkColor, string lightColor)`: Con colores personalizados

**Características técnicas:**
- **Biblioteca:** QRCoder 1.7.0
- **Nivel de corrección de errores:** ECCLevel.Q (25% de errores recuperables)
- **Formato de salida:** Base64 PNG (`data:image/png;base64,...`)
- **Cálculo de tamaño:** Ajuste automático de `pixelsPerModule` basado en tamaño deseado
- **Conversión de colores:** Hex a RGB para personalización

**Uso en el sistema:**
- Generación de QR para visualización pública (`/q/{token}`)
- QR en detalles de carnet
- QR en vista de impresión

### Auditoría (AuditService)

**Registro de acciones:**
- **Campos:** InstitutionId, UserId, Action, Entity, EntityId, Timestamp, Metadata
- **Acciones típicas:** CREATE, UPDATE, DELETE, TOGGLE_ACTIVE
- **Entidades rastreadas:** Card, EntityProfile, EventRecord, AppUser, Institution
- **Metadata:** Diccionario JSON con información adicional (ej: CardNumber, OldStatus, NewStatus)

**Ejemplos de auditoría:**
- Creación de usuario: `{ "Email": "...", "Role": "..." }`
- Toggle de carnet: `{ "CardNumber": "...", "IsActive": true }`
- Toggle de usuario: `{ "Email": "...", "OldStatus": true, "NewStatus": false }`

### Gestión de Usuarios

**Creación de Usuario:**
1. Validación de permisos según rol:
   - InstitutionAdmin: Solo puede crear Staff y AdministrativeOperator
   - SuperAdmin: Puede crear todos los roles excepto SuperAdmin (a través de código)
2. Validación de institución:
   - InstitutionAdmin: Fuerza su propia institución
   - SuperAdmin: Debe seleccionar (excepto si crea otro SuperAdmin)
   - SuperAdmin no puede tener institución asignada
3. Validación de email único
4. Creación con `EmailConfirmed = true` (no requiere confirmación)
5. Asignación de rol
6. Agregar claim `InstitutionId` si tiene institución
7. Auditoría de creación

**Protecciones:**
- No se puede desactivar el propio usuario
- Validación de rol permitido según permisos
- Validación de institución existente y activa

### Flujo Multi-Tenant Completo

**Nivel 1: Claims (AccountController)**
- Durante login, se establece claim `InstitutionId`
- Claim se guarda en Identity
- Claim se incluye en cookie de autenticación

**Nivel 2: Middleware (TenantMiddleware)**
- Se ejecuta después de Authentication, antes de Authorization
- Obtiene `InstitutionId` del claim
- Lo establece en `HttpContext.Items["TenantId"]`
- SuperAdmin no tiene tenant (null)

**Nivel 3: TenantProvider (Servicios)**
- Obtiene `InstitutionId` desde claims o `HttpContext.Items`
- Retorna `null` para SuperAdmin
- Usado por todos los servicios para filtrado

**Nivel 4: Servicios**
- Usan `ApplyTenantFilter()` extension method
- Filtran automáticamente por `InstitutionId`
- SuperAdmin ve todos los registros

**Nivel 5: DbContext (SaveChangesAsync)**
- Validación final: No permite cambiar `InstitutionId` en updates
- Restaura `InstitutionId` original si se intenta cambiar
- Lanza excepción si detecta violación multi-tenant

**Nivel 6: Base de Datos**
- Índices en `InstitutionId` para performance
- Foreign keys con `DeleteBehavior.Restrict`
- Constraints de unicidad

### Configuración de Visibilidad de Datos

**Niveles de configuración:**
1. **Global (Institution):** `PatientDataVisibilityConfig` (Dictionary<string, bool>)
2. **Por Paciente (EntityProfile):** `PatientDataVisibilityOverride` (Dictionary<string, bool>?)
   - Sobrescribe configuración global si existe
   - Null si no hay override

**Lógica de aplicación:**
- Primero se consulta `PatientDataVisibilityOverride`
- Si existe, se usa ese
- Si no existe, se usa `PatientDataVisibilityConfig` global
- Si tampoco existe, valores por defecto

**Campos configurables:**
- Nombre completo
- Identificación
- Email
- Teléfono
- Fecha de nacimiento
- Foto
- Campos personalizados (`CustomFields`)

### Generación de Números de Carnet

**Algoritmo:**
1. Obtener última tarjeta con prefijo de institución
2. Extraer número consecutivo del `CardNumber`
3. Incrementar en 1
4. Formatear con padding de 6 dígitos: `{Prefix}{Number:D6}`

**Ejemplo:**
- Último: "DEMO000045"
- Próximo: "DEMO000046"
- Si no existe: "DEMO000001"

**Características:**
- Formato fijo: Prefijo + 6 dígitos
- Búsqueda optimizada con `OrderByDescending`
- Manejo de prefijos variables (máx 10 caracteres)

### Migraciones de Base de Datos

**Migraciones existentes:**
1. **InitialCreate (20251227012057):** Creación inicial del esquema
2. **AllowNullInstitutionId (20251227015651):** Permite `InstitutionId` null en AppUser (para SuperAdmin)
3. **AddInstitutionConfigurationFields (20251227193023):** Agrega campos de configuración a Institution

**Características de migraciones:**
- Conversión automática de enums a int
- Configuración de campos JSON con serialización/deserialización
- ValueComparers personalizados para comparación de colecciones
- Índices únicos y compuestos
- Foreign keys con `OnDelete(DeleteBehavior.Restrict)`

---

*Análisis generado: Diciembre 2024*  
*Última actualización completa: Enero 2025*

