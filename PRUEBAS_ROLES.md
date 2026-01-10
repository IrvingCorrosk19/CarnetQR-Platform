# Pruebas End-to-End por Rol

## Configuración Inicial

### Usuarios de Prueba Disponibles:

1. **SuperAdmin**
   - Email: `admin@qlservices.com`
   - Password: `Admin@123456`
   - InstitutionId: `null` (no pertenece a ninguna institución)

2. **InstitutionAdmin**
   - Email: `admin@demo.com`
   - Password: `Admin@123456`
   - InstitutionId: `[ID de Empresa Demo]`

3. **Staff** (necesita crearse)
4. **AdministrativeOperator** (necesita crearse)

---

## PRUEBA 1: SuperAdmin

### Paso 1: Login
1. Navegar a: `https://localhost:7003/Account/Login`
2. Ingresar:
   - Email: `admin@qlservices.com`
   - Password: `Admin@123456`
3. Click en "Iniciar Sesión"

### Paso 2: Verificar Dashboard
- ✅ Debe mostrar el dashboard principal
- ✅ Debe tener acceso al menú de "Empresas" (Institutions)
- ✅ NO debe tener acceso a "Configuración de Institución" (InstitutionConfig)

### Paso 3: Probar Acceso a InstitutionConfig
1. Intentar acceder directamente a: `https://localhost:7003/InstitutionConfig`
2. **Resultado Esperado**: 
   - Debe redirigir a Home
   - Debe mostrar mensaje: "Los SuperAdmin no pueden acceder a la configuración de institución. Use el módulo de Instituciones para gestionar instituciones."

### Paso 4: Probar Gestión de Instituciones
1. Navegar a: `https://localhost:7003/Institutions`
2. ✅ Debe poder ver todas las instituciones
3. ✅ Debe poder crear nuevas instituciones
4. ✅ Debe poder editar instituciones
5. ✅ Debe poder activar/desactivar instituciones

### Paso 5: Probar Gestión de Usuarios
1. Navegar a: `https://localhost:7003/Users`
2. ✅ Debe poder ver todos los usuarios
3. ✅ Debe poder crear usuarios para cualquier institución
4. ✅ Debe poder asignar roles

### Paso 6: Probar Gestión de Entidades
1. Navegar a: `https://localhost:7003/EntityProfiles`
2. ✅ Debe poder ver todas las entidades de todas las instituciones
3. ✅ Debe poder crear entidades (debe seleccionar institución)
4. ✅ Debe poder editar entidades

### Paso 7: Probar Gestión de Eventos
1. Navegar a: `https://localhost:7003/Events`
2. ✅ Debe poder ver todos los eventos
3. ✅ Debe poder crear eventos (debe seleccionar institución y entidad)
4. ✅ Debe poder cambiar estado de eventos

---

## PRUEBA 2: InstitutionAdmin

### Paso 1: Login
1. Navegar a: `https://localhost:7003/Account/Login`
2. Ingresar:
   - Email: `admin@demo.com`
   - Password: `Admin@123456`
3. Click en "Iniciar Sesión"

### Paso 2: Verificar Dashboard
- ✅ Debe mostrar el dashboard principal
- ✅ NO debe tener acceso al menú de "Empresas" (Institutions)
- ✅ Debe tener acceso a "Configuración de Institución" (InstitutionConfig)

### Paso 3: Probar Acceso a InstitutionConfig
1. Navegar a: `https://localhost:7003/InstitutionConfig`
2. **Resultado Esperado**: 
   - ✅ Debe cargar correctamente
   - ✅ Debe mostrar la información de "Empresa Demo"
   - ✅ Debe mostrar 4 secciones:
     - Datos Básicos
     - Configuración del Carnet
     - QR Público
     - Visibilidad de Datos

### Paso 4: Probar Edición de Datos Básicos
1. Click en "Editar Datos"
2. ✅ Debe poder editar:
   - Nombre
   - Descripción
   - Email
   - Teléfono
   - Dirección
   - Logo (subir archivo)
3. ✅ NO debe poder editar:
   - Prefijo de Carnet
   - Tipo de Institución
   - Estado (Activa/Inactiva)
4. Guardar cambios
5. ✅ Debe mostrar mensaje de éxito

### Paso 5: Probar Configuración del Carnet
1. Click en "Configurar Carnet"
2. ✅ Debe poder:
   - Activar/desactivar foto en carnet
   - Seleccionar hasta 6 campos visibles
   - Configurar modo de visualización QR (Número de carnet o Nombre)
3. Guardar cambios
4. ✅ Debe mostrar mensaje de éxito

### Paso 6: Probar Configuración QR Público
1. Click en "Configurar QR Público"
2. ✅ Debe poder editar las instrucciones/información que se muestra en el QR
3. Guardar cambios
4. ✅ Debe mostrar mensaje de éxito

### Paso 7: Probar Visibilidad de Datos
1. Click en "Configurar Visibilidad"
2. ✅ Debe poder configurar qué datos del paciente se muestran en el QR público
3. Guardar cambios
4. ✅ Debe mostrar mensaje de éxito

### Paso 8: Probar Gestión de Usuarios
1. Navegar a: `https://localhost:7003/Users`
2. ✅ Debe poder ver solo usuarios de su institución
3. ✅ Debe poder crear usuarios para su institución
4. ✅ Debe poder asignar roles: InstitutionAdmin, Staff, AdministrativeOperator
5. ✅ NO debe poder crear SuperAdmin

### Paso 9: Probar Gestión de Entidades
1. Navegar a: `https://localhost:7003/EntityProfiles`
2. ✅ Debe poder ver solo entidades de su institución
3. ✅ Debe poder crear entidades (InstitutionId se asigna automáticamente)
4. ✅ Debe poder editar entidades de su institución
5. ✅ Debe poder subir fotos si PhotoEnabled está activo

### Paso 10: Probar Gestión de Carnets
1. Navegar a: `https://localhost:7003/Cards`
2. ✅ Debe poder ver solo carnets de su institución
3. ✅ Debe poder generar carnets para entidades de su institución
4. ✅ Debe poder activar/desactivar carnets

### Paso 11: Probar Gestión de Eventos
1. Navegar a: `https://localhost:7003/Events`
2. ✅ Debe poder ver solo eventos de su institución
3. ✅ Debe poder crear eventos (solo para entidades de su institución)
4. ✅ Debe poder cambiar estado de eventos

### Paso 12: Probar Estadísticas
1. Navegar a: `https://localhost:7003/Statistics`
2. ✅ Debe poder ver estadísticas solo de su institución

---

## PRUEBA 3: Staff

### Paso 1: Crear Usuario Staff
1. Login como InstitutionAdmin
2. Navegar a: `https://localhost:7003/Users/Create`
3. Crear usuario:
   - Email: `staff@demo.com`
   - Password: `Staff@123456`
   - Rol: `Staff`
   - Institución: `Empresa Demo` (automático)
4. Guardar

### Paso 2: Login como Staff
1. Logout
2. Login con:
   - Email: `staff@demo.com`
   - Password: `Staff@123456`

### Paso 3: Verificar Accesos
- ✅ NO debe tener acceso a "Empresas" (Institutions)
- ✅ NO debe tener acceso a "Configuración de Institución" (InstitutionConfig)
- ✅ NO debe tener acceso a "Usuarios" (Users)
- ✅ NO debe tener acceso a "Estadísticas" (Statistics)
- ✅ Debe tener acceso a:
  - Entidades (EntityProfiles)
  - Carnets (Cards)
  - Eventos (Events)

### Paso 4: Probar Gestión de Entidades
1. Navegar a: `https://localhost:7003/EntityProfiles`
2. ✅ Debe poder crear entidades
3. ✅ Debe poder editar entidades
4. ✅ Debe poder subir fotos si PhotoEnabled está activo
5. ✅ NO debe poder ver entidades de otras instituciones

### Paso 5: Probar Gestión de Carnets
1. Navegar a: `https://localhost:7003/Cards`
2. ✅ Debe poder generar carnets para entidades de su institución
3. ✅ Debe poder ver detalles de carnets

### Paso 6: Probar Gestión de Eventos
1. Navegar a: `https://localhost:7003/Events`
2. ✅ Debe poder crear eventos
3. ✅ Debe poder cambiar estado de eventos (Completado/No Completado)
4. ✅ Solo puede cambiar estado después de la fecha programada

### Paso 7: Probar Acceso Denegado
1. Intentar acceder a: `https://localhost:7003/InstitutionConfig`
2. **Resultado Esperado**: 
   - Debe mostrar error 403 (Forbidden) o redirigir a AccessDenied

---

## PRUEBA 4: AdministrativeOperator

### Paso 1: Crear Usuario AdministrativeOperator
1. Login como InstitutionAdmin
2. Navegar a: `https://localhost:7003/Users/Create`
3. Crear usuario:
   - Email: `operator@demo.com`
   - Password: `Operator@123456`
   - Rol: `AdministrativeOperator`
   - Institución: `Empresa Demo` (automático)
4. Guardar

### Paso 2: Login como AdministrativeOperator
1. Logout
2. Login con:
   - Email: `operator@demo.com`
   - Password: `Operator@123456`

### Paso 3: Verificar Accesos
- ✅ NO debe tener acceso a "Empresas" (Institutions)
- ✅ NO debe tener acceso a "Configuración de Institución" (InstitutionConfig)
- ✅ NO debe tener acceso a "Usuarios" (Users)
- ✅ NO debe tener acceso a "Estadísticas" (Statistics)
- ✅ Debe tener acceso a:
  - Entidades (EntityProfiles)
  - Carnets (Cards)
  - Eventos (Events)

### Paso 4: Probar Gestión de Entidades
1. Navegar a: `https://localhost:7003/EntityProfiles`
2. ✅ Debe poder crear entidades
3. ✅ Debe poder editar entidades

### Paso 5: Probar Gestión de Eventos
1. Navegar a: `https://localhost:7003/Events`
2. ✅ Debe poder crear eventos
3. ✅ Debe poder ver eventos
4. ❌ NO debe poder cambiar estado de eventos (Completado/No Completado)
   - Los botones de "Marcar como Completado" o "No Completado" NO deben aparecer

---

## PRUEBA 5: Verificación de Multi-Tenancy

### Paso 1: Crear Segunda Institución
1. Login como SuperAdmin
2. Crear nueva institución:
   - Nombre: "Hospital Test"
   - Prefijo: "HOSP"
   - Tipo: Hospital
   - Crear usuario admin: `admin@hospital.com` / `Admin@123456`

### Paso 2: Login como Admin de Segunda Institución
1. Logout
2. Login con: `admin@hospital.com` / `Admin@123456`

### Paso 3: Verificar Aislamiento
1. Navegar a: `https://localhost:7003/EntityProfiles`
2. ✅ Solo debe ver entidades de "Hospital Test"
3. ✅ NO debe ver entidades de "Empresa Demo"

### Paso 4: Crear Entidad en Segunda Institución
1. Crear una nueva entidad
2. ✅ El InstitutionId debe ser automáticamente de "Hospital Test"
3. ✅ No debe poder asignar InstitutionId de otra institución

### Paso 5: Verificar desde Primera Institución
1. Logout
2. Login como `admin@demo.com`
3. Navegar a: `https://localhost:7003/EntityProfiles`
4. ✅ NO debe ver la entidad creada en "Hospital Test"

---

## PRUEBA 6: Verificación de Claims y TenantProvider

### Paso 1: Verificar Claim InstitutionId
1. Login como InstitutionAdmin
2. Abrir consola del navegador (F12)
3. Verificar que el claim "InstitutionId" esté presente en la sesión
4. Navegar a: `https://localhost:7003/InstitutionConfig`
5. ✅ Debe cargar correctamente

### Paso 2: Verificar Refresh de Claims
1. Si el claim no está presente, el sistema debe:
   - Obtener InstitutionId de la entidad AppUser
   - Agregar el claim automáticamente
   - Refrescar el sign-in
   - ✅ InstitutionConfig debe funcionar

---

## Checklist de Funcionalidades por Rol

| Funcionalidad | SuperAdmin | InstitutionAdmin | Staff | AdministrativeOperator |
|--------------|------------|-----------------|-------|------------------------|
| Ver Instituciones | ✅ Todas | ❌ | ❌ | ❌ |
| Crear Instituciones | ✅ | ❌ | ❌ | ❌ |
| Editar Instituciones | ✅ | ❌ | ❌ | ❌ |
| InstitutionConfig | ❌ | ✅ | ❌ | ❌ |
| Ver Usuarios | ✅ Todos | ✅ Solo su inst. | ❌ | ❌ |
| Crear Usuarios | ✅ | ✅ Solo su inst. | ❌ | ❌ |
| Ver Entidades | ✅ Todas | ✅ Solo su inst. | ✅ Solo su inst. | ✅ Solo su inst. |
| Crear Entidades | ✅ | ✅ | ✅ | ✅ |
| Ver Carnets | ✅ Todos | ✅ Solo su inst. | ✅ Solo su inst. | ✅ Solo su inst. |
| Generar Carnets | ✅ | ✅ | ✅ | ✅ |
| Ver Eventos | ✅ Todos | ✅ Solo su inst. | ✅ Solo su inst. | ✅ Solo su inst. |
| Crear Eventos | ✅ | ✅ | ✅ | ✅ |
| Cambiar Estado Eventos | ✅ | ✅ | ✅ | ❌ |
| Ver Estadísticas | ✅ Todas | ✅ Solo su inst. | ❌ | ❌ |

---

## Notas Importantes

1. **InstitutionConfig** solo debe ser accesible para `InstitutionAdmin`
2. **SuperAdmin** debe ser redirigido con mensaje claro
3. **Staff y AdministrativeOperator** deben recibir 403 (Forbidden)
4. El sistema debe manejar automáticamente la falta de claims
5. El multi-tenancy debe estar estrictamente implementado
6. Los logs deben mostrar información detallada para debugging

---

## Comandos Útiles para Verificar

```sql
-- Verificar usuarios y sus roles
SELECT u."Email", u."InstitutionId", r."Name" as Role
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id";

-- Verificar claims de usuarios
SELECT u."Email", c."Type", c."Value"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE c."Type" = 'InstitutionId';
```


