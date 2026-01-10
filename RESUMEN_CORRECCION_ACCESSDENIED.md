# ✅ Corrección Completa: Error 404 en /Account/AccessDenied

## 🔍 Problema Identificado

**Error**: `HTTP ERROR 404` en `https://localhost:7003/Account/AccessDenied?ReturnUrl=%2FInstitutionConfig`

**Causa Raíz**:
1. El `InstitutionConfigController` requiere rol `InstitutionAdmin` para acceder
2. El SuperAdmin NO tiene rol `InstitutionAdmin`, solo tiene `SuperAdmin`
3. Cuando ASP.NET Core Identity detecta falta de permisos, redirige automáticamente a `/Account/AccessDenied`
4. **La vista `AccessDenied.cshtml` NO existía**, causando el error 404

## ✅ Correcciones Implementadas

### 1. **Creada Vista AccessDenied.cshtml** ✅
- Ubicación: `CarnetQRPlatform.Web/Views/Account/AccessDenied.cshtml`
- Vista completa con mensaje claro para SuperAdmin
- Instrucciones específicas según el tipo de usuario
- Enlaces a módulos apropiados
- Información de debugging (roles, URL solicitada)

### 2. **Agregada Acción AccessDenied en AccountController** ✅
- Método `[HttpGet] [AllowAnonymous] AccessDenied(string? returnUrl = null)`
- Obtiene roles del usuario autenticado
- Pasa información a la vista via ViewData
- Logging adecuado para debugging

### 3. **Modificado InstitutionConfigController** ✅
- Cambiado atributo de `[Authorize(Roles = Roles.InstitutionAdmin)]` a `[Authorize]`
- Validación de rol ahora es interna en `GetCurrentInstitutionAsync`
- SuperAdmin se redirige manualmente a `AccessDenied` con mensaje claro
- Validación adicional: verifica que el usuario tenga rol InstitutionAdmin

### 4. **Corregido Menú Lateral** ✅
- SuperAdmin **YA NO** ve el enlace a "Configuración"
- Solo `InstitutionAdmin` (que NO es SuperAdmin) ve "Configuración"
- Evita confusión y accesos innecesarios

### 5. **Mejorado DbInitializer** ✅
- Verifica que SuperAdmin existente tenga el rol correcto
- Si falta el rol, lo agrega automáticamente
- Verifica que InstitutionId sea NULL para SuperAdmin
- Verifica que el usuario esté activo
- Logging detallado de todas las verificaciones

## 🔧 Verificación de Base de Datos

### Script SQL para Verificar SuperAdmin:

```sql
-- Verificar roles del SuperAdmin
SELECT 
    u."UserName",
    u."Email",
    r."Name" as "Role",
    u."InstitutionId",
    u."IsActive"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@qlservices.com';
```

**Resultado Esperado:**
- Email: `admin@qlservices.com`
- Role: `SuperAdmin`
- InstitutionId: `NULL`
- IsActive: `true`

### Si el SuperAdmin NO tiene rol, ejecutar:

```sql
DO $$
DECLARE
    user_id TEXT;
    role_id TEXT;
BEGIN
    SELECT "Id" INTO user_id FROM "AspNetUsers" WHERE "Email" = 'admin@qlservices.com';
    SELECT "Id" INTO role_id FROM "AspNetRoles" WHERE "NormalizedName" = 'SUPERADMIN';
    
    IF user_id IS NOT NULL AND role_id IS NOT NULL THEN
        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
        VALUES (user_id, role_id)
        ON CONFLICT DO NOTHING;
        
        RAISE NOTICE 'Rol SuperAdmin asignado exitosamente';
    END IF;
END $$;
```

## 🎯 Comportamiento Esperado Ahora

### Como SuperAdmin:
1. ✅ El menú **NO muestra** "Configuración"
2. ✅ Si accede directamente a `/InstitutionConfig` vía URL:
   - Se redirige a `/Account/AccessDenied`
   - Ve mensaje claro: "Como SuperAdmin, no puede acceder a la configuración de instituciones específicas"
   - Ve enlace a módulo de Instituciones
   - Ve botones para navegar
3. ✅ Puede usar el módulo de "Instituciones" para gestionar todas las instituciones

### Como InstitutionAdmin:
1. ✅ El menú **SÍ muestra** "Configuración"
2. ✅ Puede acceder a `/InstitutionConfig` normalmente
3. ✅ Ve la configuración de SU institución

## 📝 Archivos Modificados/Creados

### Modificados:
1. ✅ `AccountController.cs` - Agregada acción AccessDenied
2. ✅ `InstitutionConfigController.cs` - Validación mejorada y redirección correcta
3. ✅ `_AdminLayout.cshtml` - Menú corregido (SuperAdmin no ve Configuración)
4. ✅ `DbInitializer.cs` - Verificación y corrección automática de roles

### Creados:
1. ✅ `AccessDenied.cshtml` - Vista completa de acceso denegado
2. ✅ `CORRECCION_ACCESSDENIED.md` - Documentación del problema
3. ✅ `VERIFICAR_ROLES_USUARIO.sql` - Script SQL para verificar roles
4. ✅ `RESUMEN_CORRECCION_ACCESSDENIED.md` - Este documento

## ✅ Estado Final

- ✅ Vista AccessDenied creada y funcionando
- ✅ Acción AccessDenied implementada correctamente
- ✅ Validación de roles corregida
- ✅ Menú lateral corregido
- ✅ DbInitializer mejorado para verificar roles
- ✅ Compilación exitosa sin errores
- ✅ No hay JavaScript bloqueando (verificado)
- ✅ Configuración de cookie correcta (`AccessDeniedPath = "/Account/AccessDenied"`)

## 🧪 Pruebas Recomendadas

1. **Login como SuperAdmin:**
   - Verificar que NO aparece "Configuración" en el menú
   - Intentar acceder a `/InstitutionConfig` directamente
   - Debe redirigir a `/Account/AccessDenied` (NO 404)
   - Debe mostrar mensaje claro

2. **Login como InstitutionAdmin:**
   - Verificar que SÍ aparece "Configuración" en el menú
   - Acceder a `/InstitutionConfig`
   - Debe funcionar normalmente

3. **Verificar Base de Datos:**
   - Ejecutar script SQL para verificar roles
   - Si falta rol SuperAdmin, agregarlo
   - Reiniciar aplicación si fue necesario modificar BD

## 🎉 Resultado

**El error 404 en `/Account/AccessDenied` está completamente corregido.**

Ahora cuando un SuperAdmin intenta acceder a `/InstitutionConfig`:
- ✅ Se redirige correctamente a `/Account/AccessDenied`
- ✅ Ve mensaje claro y útil
- ✅ Tiene enlaces para navegar apropiadamente
- ✅ NO recibe error 404

