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

- **Framework:** .NET 8.0
- **ORM:** Entity Framework Core
- **Base de Datos:** PostgreSQL
- **Autenticación:** ASP.NET Core Identity
- **Logging:** Serilog
- **Patrón:** Repository/Service Pattern
- **Arquitectura:** Clean Architecture / Layered Architecture

---

*Análisis generado: Diciembre 2024*

