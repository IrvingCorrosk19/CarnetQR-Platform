# Análisis del Problema de Login - Usuario Creado desde Institutions/Create

## Usuario de Prueba
- **Email**: `aloticopty@tico.com`
- **Creado desde**: `/Institutions/Create`

## Flujo de Creación del Usuario

### 1. Creación en InstitutionsController.Create()

```csharp
var adminUser = new AppUser
{
    UserName = model.AdminEmail,        // "aloticopty@tico.com"
    Email = model.AdminEmail,           // "aloticopty@tico.com"
    FirstName = model.AdminFirstName,
    LastName = model.AdminLastName,
    InstitutionId = createdInstitution.Id,
    IsActive = true,
    EmailConfirmed = true
};

var createUserResult = await _userManager.CreateAsync(adminUser, model.AdminPassword);
```

**Puntos importantes:**
- `UserName` = `Email` (ambos son el mismo valor)
- `EmailConfirmed = true` (no requiere confirmación)
- `IsActive = true` (usuario activo)
- `InstitutionId` se asigna correctamente

### 2. Asignación de Rol

```csharp
await _userManager.AddToRoleAsync(adminUser, Roles.InstitutionAdmin);
```

### 3. Asignación de Claim

```csharp
await _userManager.AddClaimAsync(adminUser, new Claim("InstitutionId", createdInstitution.Id.ToString()));
```

## Flujo de Login

### 1. Búsqueda del Usuario

```csharp
var user = await _userManager.FindByEmailAsync(model.Email);
```

**Busca por**: `Email` o `NormalizedEmail`

### 2. Verificación de Estado

```csharp
if (user == null || !user.IsActive)
{
    // Error: Credenciales inválidas
}
```

### 3. Intento de Login

```csharp
var result = await _signInManager.PasswordSignInAsync(
    user.UserName!,  // Usa UserName, no Email
    model.Password,
    model.RememberMe,
    lockoutOnFailure: true);
```

**IMPORTANTE**: `PasswordSignInAsync` usa `user.UserName`, no `user.Email`

## Posibles Problemas

### Problema 1: Password no cumple requisitos
**Requisitos de Password:**
- Mínimo 8 caracteres
- Debe tener al menos 1 dígito
- Debe tener al menos 1 minúscula
- Debe tener al menos 1 mayúscula
- Debe tener al menos 1 carácter no alfanumérico (símbolo)

**Solución**: Verificar que el password cumpla todos los requisitos.

### Problema 2: Usuario no se creó correctamente
**Verificar en BD:**
```sql
SELECT * FROM "AspNetUsers" WHERE "Email" = 'aloticopty@tico.com';
```

**Verificar:**
- ✅ `PasswordHash` no es NULL
- ✅ `EmailConfirmed` = true
- ✅ `IsActive` = true
- ✅ `InstitutionId` no es NULL

### Problema 3: Rol no asignado
**Verificar en BD:**
```sql
SELECT u."Email", r."Name" as Role
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'aloticopty@tico.com';
```

**Debe mostrar**: `InstitutionAdmin`

### Problema 4: Claim no asignado
**Verificar en BD:**
```sql
SELECT u."Email", c."Type", c."Value"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE u."Email" = 'aloticopty@tico.com' AND c."Type" = 'InstitutionId';
```

**Debe mostrar**: `InstitutionId` con el valor del GUID de la institución.

### Problema 5: Usuario bloqueado
**Verificar en BD:**
```sql
SELECT "Email", "LockoutEnd", "AccessFailedCount", "LockoutEnabled"
FROM "AspNetUsers"
WHERE "Email" = 'aloticopty@tico.com';
```

**Si `LockoutEnd` no es NULL y es mayor que NOW()**: Usuario está bloqueado.

### Problema 6: Normalización de Email/UserName
ASP.NET Identity normaliza emails y usernames a mayúsculas.

**Verificar:**
```sql
SELECT "Email", "NormalizedEmail", "UserName", "NormalizedUserName"
FROM "AspNetUsers"
WHERE "Email" = 'aloticopty@tico.com';
```

**Debe ser:**
- `NormalizedEmail` = `ALOTICOPTY@TICO.COM`
- `NormalizedUserName` = `ALOTICOPTY@TICO.COM`

## Scripts de Verificación Completa

### Script 1: Verificar Usuario Completo
```sql
-- Verificar usuario completo
SELECT 
    u."Id",
    u."UserName",
    u."NormalizedUserName",
    u."Email",
    u."NormalizedEmail",
    u."EmailConfirmed",
    u."IsActive",
    u."InstitutionId",
    u."FirstName",
    u."LastName",
    u."LockoutEnabled",
    u."LockoutEnd",
    u."AccessFailedCount",
    CASE 
        WHEN u."PasswordHash" IS NULL THEN 'ERROR: Sin contraseña'
        ELSE 'OK: Tiene contraseña'
    END as PasswordStatus,
    i."Name" as InstitutionName
FROM "AspNetUsers" u
LEFT JOIN "Institutions" i ON u."InstitutionId" = i."Id"
WHERE u."Email" = 'aloticopty@tico.com';
```

### Script 2: Verificar Roles
```sql
-- Verificar roles
SELECT 
    u."Email",
    r."Name" as RoleName,
    r."NormalizedName" as NormalizedRoleName
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'aloticopty@tico.com';
```

### Script 3: Verificar Claims
```sql
-- Verificar claims
SELECT 
    u."Email",
    c."Type" as ClaimType,
    c."Value" as ClaimValue
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE u."Email" = 'aloticopty@tico.com';
```

### Script 4: Verificar Institución
```sql
-- Verificar institución asociada
SELECT 
    u."Email",
    u."InstitutionId",
    i."Name" as InstitutionName,
    i."IsActive" as InstitutionIsActive,
    i."CardPrefix"
FROM "AspNetUsers" u
LEFT JOIN "Institutions" i ON u."InstitutionId" = i."Id"
WHERE u."Email" = 'aloticopty@tico.com';
```

## Soluciones Posibles

### Solución 1: Resetear Password
Si el usuario existe pero el password no funciona, se puede resetear desde la BD o crear un endpoint de administración.

### Solución 2: Verificar Logs
Revisar los logs de la aplicación cuando se intenta hacer login:
- Buscar: `"Login attempt for email: aloticopty@tico.com"`
- Ver qué mensaje aparece después

### Solución 3: Verificar Errores de Creación
Si el usuario no se creó correctamente, revisar los logs cuando se creó la institución:
- Buscar: `"Error creating InstitutionAdmin"`
- Ver los errores específicos

### Solución 4: Recrear Usuario Manualmente
Si el usuario tiene problemas, se puede:
1. Eliminar el usuario de la BD
2. Volver a crear la institución (si no tiene datos importantes)
3. O crear el usuario manualmente desde `/Users/Create`

## Checklist de Diagnóstico

- [ ] Usuario existe en `AspNetUsers`
- [ ] `PasswordHash` no es NULL
- [ ] `EmailConfirmed` = true
- [ ] `IsActive` = true
- [ ] `InstitutionId` no es NULL y existe en `Institutions`
- [ ] Tiene rol `InstitutionAdmin` asignado
- [ ] Tiene claim `InstitutionId` asignado
- [ ] `LockoutEnd` es NULL o está en el pasado
- [ ] `AccessFailedCount` < 5
- [ ] Password cumple todos los requisitos
- [ ] `NormalizedEmail` y `NormalizedUserName` están correctos

## Mejoras Implementadas

1. **Mejor logging en InstitutionsController**: Ahora muestra errores detallados cuando falla la creación del usuario
2. **Mejor logging en AccountController**: Ahora muestra información detallada sobre por qué falla el login
3. **Mensajes de error más claros**: Diferencia entre "usuario no encontrado", "usuario inactivo", "usuario bloqueado", etc.

## Próximos Pasos

1. Ejecutar los scripts SQL para verificar el estado del usuario
2. Revisar los logs de la aplicación
3. Verificar que el password cumpla todos los requisitos
4. Si el usuario no existe o tiene problemas, recrearlo


