-- Script para verificar el usuario creado
-- Ejecutar en PostgreSQL para verificar el estado del usuario

-- 1. Verificar si el usuario existe
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
    u."PasswordHash" IS NOT NULL as HasPassword
FROM "AspNetUsers" u
WHERE u."Email" = 'aloticopty@tico.com' 
   OR u."UserName" = 'aloticopty@tico.com'
   OR u."NormalizedEmail" = 'ALOTICOPTY@TICO.COM';

-- 2. Verificar roles asignados
SELECT 
    u."Email",
    r."Name" as RoleName
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'aloticopty@tico.com' 
   OR u."UserName" = 'aloticopty@tico.com';

-- 3. Verificar claims asignados
SELECT 
    u."Email",
    c."Type" as ClaimType,
    c."Value" as ClaimValue
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE u."Email" = 'aloticopty@tico.com' 
   OR u."UserName" = 'aloticopty@tico.com';

-- 4. Verificar la institución asociada
SELECT 
    u."Email",
    u."InstitutionId",
    i."Name" as InstitutionName,
    i."IsActive" as InstitutionIsActive
FROM "AspNetUsers" u
LEFT JOIN "Institutions" i ON u."InstitutionId" = i."Id"
WHERE u."Email" = 'aloticopty@tico.com' 
   OR u."UserName" = 'aloticopty@tico.com';

-- 5. Verificar si hay problemas comunes
SELECT 
    u."Email",
    CASE 
        WHEN u."PasswordHash" IS NULL THEN 'ERROR: No tiene contraseña'
        ELSE 'OK: Tiene contraseña'
    END as PasswordStatus,
    CASE 
        WHEN u."EmailConfirmed" = false THEN 'ERROR: Email no confirmado'
        ELSE 'OK: Email confirmado'
    END as EmailStatus,
    CASE 
        WHEN u."IsActive" = false THEN 'ERROR: Usuario inactivo'
        ELSE 'OK: Usuario activo'
    END as ActiveStatus,
    CASE 
        WHEN u."LockoutEnd" IS NOT NULL AND u."LockoutEnd" > NOW() THEN 'ERROR: Usuario bloqueado'
        ELSE 'OK: Usuario no bloqueado'
    END as LockoutStatus,
    CASE 
        WHEN NOT EXISTS (SELECT 1 FROM "AspNetUserRoles" ur WHERE ur."UserId" = u."Id") THEN 'ERROR: No tiene roles asignados'
        ELSE 'OK: Tiene roles'
    END as RoleStatus,
    CASE 
        WHEN u."InstitutionId" IS NULL THEN 'WARNING: No tiene InstitutionId'
        WHEN NOT EXISTS (SELECT 1 FROM "Institutions" i WHERE i."Id" = u."InstitutionId") THEN 'ERROR: InstitutionId no existe'
        ELSE 'OK: InstitutionId válido'
    END as InstitutionStatus
FROM "AspNetUsers" u
WHERE u."Email" = 'aloticopty@tico.com' 
   OR u."UserName" = 'aloticopty@tico.com';


