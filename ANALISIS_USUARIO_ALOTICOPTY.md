# Análisis: Usuario aloticopty@tico.com no puede ingresar

## Problema Identificado

Según los logs, el error principal es:

```
duplicate key value violates unique constraint "IX_Institutions_CardPrefix"
```

**Esto significa que:**
- La institución NO se creó porque el `CardPrefix` ya existe
- Como la institución no se creó, el usuario `aloticopty@tico.com` tampoco se creó
- Por eso no puede hacer login: **el usuario no existe en la base de datos**

## Solución Implementada

### 1. Validación Preventiva de CardPrefix
Ahora el sistema verifica ANTES de intentar guardar si el `CardPrefix` ya existe y muestra un mensaje claro.

### 2. Mejor Manejo de Errores
- Captura específica del error de `CardPrefix` duplicado
- Mensajes de error más claros para el usuario
- Logging mejorado

### 3. Endpoint de Consulta
Se agregó un endpoint para consultar usuarios directamente desde la aplicación.

## Cómo Consultar los Usuarios

### Opción 1: Usar el Endpoint de Prueba

Navegar a: `https://localhost:7003/Test/CompareUsers`

Este endpoint mostrará una comparación detallada entre:
- `aloticopty@tico.com`
- `admin@qlservices.com`

Incluye:
- Información básica (Email, UserName, IsActive, etc.)
- Roles asignados
- Claims asignados
- Instituciones asociadas
- Análisis de problemas

### Opción 2: Usar el Endpoint General

Navegar a: `https://localhost:7003/Test/CheckUsers`

Este endpoint muestra TODOS los usuarios con su información completa.

## Verificación en Base de Datos

### Script SQL para verificar si el usuario existe:

```sql
-- Verificar si el usuario existe
SELECT 
    u."Email",
    u."UserName",
    u."IsActive",
    u."EmailConfirmed",
    u."InstitutionId",
    CASE WHEN u."PasswordHash" IS NULL THEN 'SIN CONTRASEÑA' ELSE 'TIENE CONTRASEÑA' END as PasswordStatus,
    (SELECT string_agg(r."Name", ', ') 
     FROM "AspNetUserRoles" ur 
     JOIN "AspNetRoles" r ON ur."RoleId" = r."Id" 
     WHERE ur."UserId" = u."Id") as Roles
FROM "AspNetUsers" u
WHERE u."Email" = 'aloticopty@tico.com';
```

### Si el usuario NO existe:

El problema es que la institución no se creó debido a un `CardPrefix` duplicado. Necesitas:

1. **Verificar qué CardPrefix usaste** cuando intentaste crear la institución
2. **Verificar qué CardPrefix ya existen** en la BD:

```sql
SELECT "Name", "CardPrefix", "IsActive"
FROM "Institutions"
ORDER BY "CardPrefix";
```

3. **Usar un CardPrefix diferente** que no esté en uso

## Pasos para Solucionar

### Paso 1: Verificar CardPrefix Disponibles
```sql
SELECT "CardPrefix", "Name" 
FROM "Institutions" 
ORDER BY "CardPrefix";
```

### Paso 2: Crear Institución con CardPrefix Único
- Ir a: `https://localhost:7003/Institutions/Create`
- Usar un `CardPrefix` que NO esté en la lista anterior
- Completar todos los datos
- El sistema ahora mostrará un error claro si el CardPrefix está duplicado

### Paso 3: Verificar que el Usuario se Creó
```sql
SELECT "Email", "UserName", "IsActive", "InstitutionId"
FROM "AspNetUsers"
WHERE "Email" = 'aloticopty@tico.com';
```

### Paso 4: Verificar Rol y Claims
```sql
-- Verificar rol
SELECT u."Email", r."Name" as Role
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'aloticopty@tico.com";

-- Verificar claim
SELECT u."Email", c."Type", c."Value"
FROM "AspNetUsers" u
JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE u."Email" = 'aloticopty@tico.com' AND c."Type" = 'InstitutionId';
```

## Comparación Esperada

### admin@qlservices.com (SuperAdmin - FUNCIONA)
- ✅ Existe en BD
- ✅ Tiene password
- ✅ EmailConfirmed = true
- ✅ IsActive = true
- ✅ InstitutionId = NULL (correcto para SuperAdmin)
- ✅ Rol: SuperAdmin
- ✅ Puede hacer login

### aloticopty@tico.com (InstitutionAdmin - NO FUNCIONA)
- ❌ Probablemente NO existe en BD (porque la institución no se creó)
- ❌ O si existe, puede tener problemas:
  - Sin password
  - Sin rol asignado
  - Sin claim InstitutionId
  - InstitutionId inválido

## Mejoras Implementadas

1. **Validación de CardPrefix antes de guardar**: Evita el error de duplicado
2. **Mensajes de error claros**: Indica qué CardPrefix está duplicado
3. **Mejor logging**: Registra todos los pasos de creación
4. **Endpoint de comparación**: Facilita el diagnóstico

## Próximos Pasos

1. **Acceder a**: `https://localhost:7003/Test/CompareUsers` para ver la comparación
2. **Verificar CardPrefix disponibles** antes de crear una nueva institución
3. **Crear la institución con un CardPrefix único**
4. **Verificar que el usuario se creó correctamente**
5. **Intentar login nuevamente**

