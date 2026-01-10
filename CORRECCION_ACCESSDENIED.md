# Corrección: Error 404 en /Account/AccessDenied

## 🔍 Problema Identificado

El error 404 en `/Account/AccessDenied` ocurre cuando un SuperAdmin intenta acceder a `/InstitutionConfig`:

1. **El controlador `InstitutionConfigController`** requiere rol `InstitutionAdmin` (atributo `[Authorize(Roles = Roles.InstitutionAdmin)]`)
2. **El SuperAdmin NO tiene el rol InstitutionAdmin**, solo tiene rol `SuperAdmin`
3. **ASP.NET Core Identity** automáticamente redirige a `/Account/AccessDenied` cuando detecta falta de permisos
4. **La vista `AccessDenied.cshtml` NO existía**, causando el error 404

## ✅ Correcciones Implementadas

### 1. **Creada acción AccessDenied en AccountController** ✅
- Método `AccessDenied` con parámetro `returnUrl`
- Manejo de roles del usuario
- Logging adecuado

### 2. **Creada vista AccessDenied.cshtml** ✅
- Vista con mensaje claro para SuperAdmin
- Instrucciones específicas para SuperAdmin
- Enlaces a módulos apropiados (Instituciones)
- Información de roles y URL solicitada para debugging

### 3. **Modificado InstitutionConfigController** ✅
- Cambiado atributo de `[Authorize(Roles = Roles.InstitutionAdmin)]` a `[Authorize]`
- Validación de rol Interna en `GetCurrentInstitutionAsync`
- Redirección manual a `AccessDenied` cuando es SuperAdmin
- Mensaje claro en la vista AccessDenied

### 4. **Corregido menú lateral** ✅
- SuperAdmin ya NO ve el enlace a "Configuración" (usa Instituciones)
- Solo InstitutionAdmin ve el enlace a Configuración
- Evita confusión y acceso innecesario

## 🔧 Verificaciones Necesarias

### 1. Verificar que SuperAdmin tenga el rol correcto en BD

Ejecutar este script SQL en PostgreSQL:

```sql
-- Verificar roles del usuario SuperAdmin
SELECT 
    u."UserName",
    u."Email",
    r."Name" as "Role",
    u."InstitutionId"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@qlservices.com';
```

**Resultado Esperado:**
- Usuario: `admin@qlservices.com`
- Rol: `SuperAdmin`
- InstitutionId: `NULL` (SuperAdmin no tiene institución)

### 2. Si el SuperAdmin NO tiene rol, ejecutar:

```sql
DO $$
DECLARE
    user_id TEXT;
    role_id TEXT;
BEGIN
    -- Obtener ID del usuario SuperAdmin
    SELECT "Id" INTO user_id 
    FROM "AspNetUsers" 
    WHERE "Email" = 'admin@qlservices.com';
    
    -- Obtener ID del rol SuperAdmin
    SELECT "Id" INTO role_id 
    FROM "AspNetRoles" 
    WHERE "NormalizedName" = 'SUPERADMIN';
    
    -- Asignar rol si no existe
    IF user_id IS NOT NULL AND role_id IS NOT NULL THEN
        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
        VALUES (user_id, role_id)
        ON CONFLICT DO NOTHING;
        
        RAISE NOTICE 'Rol SuperAdmin asignado al usuario admin@qlservices.com';
    ELSE
        RAISE NOTICE 'Usuario o rol no encontrado. Verificar que existan.';
    END IF;
END $$;
```

### 3. Verificar que todos los roles existan:

```sql
SELECT "Id", "Name", "NormalizedName" 
FROM "AspNetRoles" 
ORDER BY "Name";
```

**Deben existir:**
- SuperAdmin
- InstitutionAdmin
- Staff
- AdministrativeOperator

## 🎯 Comportamiento Esperado

### SuperAdmin accediendo a /InstitutionConfig:
1. ✅ El menú NO muestra "Configuración" (ya corregido)
2. ✅ Si accede directamente vía URL, se redirige a `/Account/AccessDenied`
3. ✅ La vista AccessDenied muestra mensaje claro:
   - "Como SuperAdmin, no puede acceder a la configuración de instituciones específicas"
   - Enlace a módulo de Instituciones
   - Botón para volver al inicio

### InstitutionAdmin accediendo a /InstitutionConfig:
1. ✅ El menú SÍ muestra "Configuración"
2. ✅ Puede acceder normalmente
3. ✅ Ve la configuración de SU institución

## 📝 Archivos Modificados

1. ✅ `CarnetQRPlatform.Web/Controllers/AccountController.cs` - Agregada acción AccessDenied
2. ✅ `CarnetQRPlatform.Web/Views/Account/AccessDenied.cshtml` - Vista creada
3. ✅ `CarnetQRPlatform.Web/Controllers/InstitutionConfigController.cs` - Validación mejorada
4. ✅ `CarnetQRPlatform.Web/Views/Shared/_AdminLayout.cshtml` - Menú corregido

## 🧪 Pruebas

1. **Como SuperAdmin:**
   - Login con `admin@qlservices.com`
   - Verificar que NO aparece "Configuración" en el menú
   - Intentar acceder directamente a `/InstitutionConfig`
   - Debe redirigir a `/Account/AccessDenied` con mensaje claro

2. **Como InstitutionAdmin:**
   - Login con `admin@demo.com`
   - Verificar que SÍ aparece "Configuración" en el menú
   - Acceder a `/InstitutionConfig`
   - Debe funcionar normalmente

## ✅ Estado

- ✅ Vista AccessDenied creada
- ✅ Acción AccessDenied implementada
- ✅ Validación de roles corregida
- ✅ Menú lateral corregido
- ✅ Compilación exitosa
- ⏳ **PENDIENTE**: Verificar roles en base de datos

