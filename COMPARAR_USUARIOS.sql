-- Script para comparar los dos usuarios y encontrar diferencias
-- Usuario 1: aloticopty@tico.com (InstitutionAdmin)
-- Usuario 2: admin@qlservices.com (SuperAdmin)

-- ============================================
-- COMPARACIÓN COMPLETA DE USUARIOS
-- ============================================

-- 1. Información básica de ambos usuarios
SELECT 
    '=== INFORMACIÓN BÁSICA ===' as Seccion,
    u."Email",
    u."UserName",
    u."NormalizedEmail",
    u."NormalizedUserName",
    u."EmailConfirmed",
    u."IsActive",
    u."InstitutionId",
    u."FirstName",
    u."LastName",
    u."LockoutEnabled",
    u."LockoutEnd",
    u."AccessFailedCount",
    CASE 
        WHEN u."PasswordHash" IS NULL THEN '❌ SIN CONTRASEÑA'
        ELSE '✅ TIENE CONTRASEÑA'
    END as PasswordStatus,
    u."SecurityStamp",
    u."ConcurrencyStamp"
FROM "AspNetUsers" u
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email";

-- 2. Comparación de Roles
SELECT 
    '=== ROLES ASIGNADOS ===' as Seccion,
    u."Email",
    r."Name" as RoleName,
    r."NormalizedName" as NormalizedRoleName,
    CASE 
        WHEN r."Name" = 'SuperAdmin' THEN '🔴 SuperAdmin'
        WHEN r."Name" = 'InstitutionAdmin' THEN '🟢 InstitutionAdmin'
        WHEN r."Name" = 'Staff' THEN '🟡 Staff'
        WHEN r."Name" = 'AdministrativeOperator' THEN '🟠 AdministrativeOperator'
        ELSE r."Name"
    END as RoleDisplay
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email", r."Name";

-- 3. Comparación de Claims
SELECT 
    '=== CLAIMS ASIGNADOS ===' as Seccion,
    u."Email",
    c."Type" as ClaimType,
    c."Value" as ClaimValue
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email", c."Type";

-- 4. Verificar Instituciones asociadas
SELECT 
    '=== INSTITUCIONES ASOCIADAS ===' as Seccion,
    u."Email",
    u."InstitutionId",
    i."Name" as InstitutionName,
    i."IsActive" as InstitutionIsActive,
    i."CardPrefix",
    CASE 
        WHEN u."InstitutionId" IS NULL THEN '⚠️ Sin institución (SuperAdmin)'
        WHEN i."Id" IS NULL THEN '❌ InstitutionId no existe en tabla Institutions'
        WHEN i."IsActive" = false THEN '⚠️ Institución inactiva'
        ELSE '✅ Institución válida y activa'
    END as InstitutionStatus
FROM "AspNetUsers" u
LEFT JOIN "Institutions" i ON u."InstitutionId" = i."Id"
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email";

-- 5. Análisis de problemas potenciales
SELECT 
    '=== ANÁLISIS DE PROBLEMAS ===' as Seccion,
    u."Email",
    CASE 
        WHEN u."PasswordHash" IS NULL THEN '❌ PROBLEMA: No tiene contraseña'
        ELSE '✅ OK: Tiene contraseña'
    END as Problema1,
    CASE 
        WHEN u."EmailConfirmed" = false THEN '❌ PROBLEMA: Email no confirmado'
        ELSE '✅ OK: Email confirmado'
    END as Problema2,
    CASE 
        WHEN u."IsActive" = false THEN '❌ PROBLEMA: Usuario inactivo'
        ELSE '✅ OK: Usuario activo'
    END as Problema3,
    CASE 
        WHEN u."LockoutEnd" IS NOT NULL AND u."LockoutEnd" > NOW() THEN '❌ PROBLEMA: Usuario bloqueado hasta ' || u."LockoutEnd"::text
        WHEN u."AccessFailedCount" >= 5 THEN '⚠️ ADVERTENCIA: ' || u."AccessFailedCount" || ' intentos fallidos'
        ELSE '✅ OK: No bloqueado'
    END as Problema4,
    CASE 
        WHEN NOT EXISTS (SELECT 1 FROM "AspNetUserRoles" ur WHERE ur."UserId" = u."Id") THEN '❌ PROBLEMA: No tiene roles asignados'
        ELSE '✅ OK: Tiene roles'
    END as Problema5,
    CASE 
        WHEN u."InstitutionId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "Institutions" i WHERE i."Id" = u."InstitutionId") THEN '❌ PROBLEMA: InstitutionId no existe'
        WHEN u."InstitutionId" IS NOT NULL AND EXISTS (SELECT 1 FROM "Institutions" i WHERE i."Id" = u."InstitutionId" AND i."IsActive" = false) THEN '⚠️ ADVERTENCIA: Institución inactiva'
        ELSE '✅ OK: Institución válida o es SuperAdmin'
    END as Problema6,
    CASE 
        WHEN u."InstitutionId" IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM "AspNetUserClaims" c 
            WHERE c."UserId" = u."Id" AND c."Type" = 'InstitutionId'
        ) THEN '⚠️ ADVERTENCIA: No tiene claim InstitutionId (se agregará en login)'
        ELSE '✅ OK: Tiene claim InstitutionId o es SuperAdmin'
    END as Problema7
FROM "AspNetUsers" u
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email";

-- 6. Comparación lado a lado (formato tabla)
SELECT 
    '=== COMPARACIÓN LADO A LADO ===' as Seccion,
    'Propiedad' as Propiedad,
    MAX(CASE WHEN u."Email" = 'aloticopty@tico.com' THEN 
        CASE 
            WHEN "Propiedad" = 'Email' THEN u."Email"
            WHEN "Propiedad" = 'UserName' THEN u."UserName"
            WHEN "Propiedad" = 'EmailConfirmed' THEN u."EmailConfirmed"::text
            WHEN "Propiedad" = 'IsActive' THEN u."IsActive"::text
            WHEN "Propiedad" = 'InstitutionId' THEN COALESCE(u."InstitutionId"::text, 'NULL')
            WHEN "Propiedad" = 'PasswordHash' THEN CASE WHEN u."PasswordHash" IS NULL THEN 'NULL' ELSE 'EXISTS' END
            WHEN "Propiedad" = 'LockoutEnd' THEN COALESCE(u."LockoutEnd"::text, 'NULL')
            WHEN "Propiedad" = 'AccessFailedCount' THEN u."AccessFailedCount"::text
            WHEN "Propiedad" = 'Roles' THEN (
                SELECT string_agg(r."Name", ', ')
                FROM "AspNetUserRoles" ur
                JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
                WHERE ur."UserId" = u."Id"
            )
            WHEN "Propiedad" = 'Claims' THEN (
                SELECT string_agg(c."Type" || '=' || c."Value", ', ')
                FROM "AspNetUserClaims" c
                WHERE c."UserId" = u."Id"
            )
        END
    END) as aloticopty_tico_com,
    MAX(CASE WHEN u."Email" = 'admin@qlservices.com' THEN 
        CASE 
            WHEN "Propiedad" = 'Email' THEN u."Email"
            WHEN "Propiedad" = 'UserName' THEN u."UserName"
            WHEN "Propiedad" = 'EmailConfirmed' THEN u."EmailConfirmed"::text
            WHEN "Propiedad" = 'IsActive' THEN u."IsActive"::text
            WHEN "Propiedad" = 'InstitutionId' THEN COALESCE(u."InstitutionId"::text, 'NULL')
            WHEN "Propiedad" = 'PasswordHash' THEN CASE WHEN u."PasswordHash" IS NULL THEN 'NULL' ELSE 'EXISTS' END
            WHEN "Propiedad" = 'LockoutEnd' THEN COALESCE(u."LockoutEnd"::text, 'NULL')
            WHEN "Propiedad" = 'AccessFailedCount' THEN u."AccessFailedCount"::text
            WHEN "Propiedad" = 'Roles' THEN (
                SELECT string_agg(r."Name", ', ')
                FROM "AspNetUserRoles" ur
                JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
                WHERE ur."UserId" = u."Id"
            )
            WHEN "Propiedad" = 'Claims' THEN (
                SELECT string_agg(c."Type" || '=' || c."Value", ', ')
                FROM "AspNetUserClaims" c
                WHERE c."UserId" = u."Id"
            )
        END
    END) as admin_qlservices_com
FROM "AspNetUsers" u
CROSS JOIN (SELECT unnest(ARRAY['Email', 'UserName', 'EmailConfirmed', 'IsActive', 'InstitutionId', 'PasswordHash', 'LockoutEnd', 'AccessFailedCount', 'Roles', 'Claims']) as "Propiedad") props
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
GROUP BY props."Propiedad"
ORDER BY props."Propiedad";

-- Versión simplificada de comparación
SELECT 
    u."Email",
    u."PasswordHash" IS NOT NULL as HasPassword,
    u."EmailConfirmed",
    u."IsActive",
    u."InstitutionId",
    u."LockoutEnd",
    u."AccessFailedCount",
    (SELECT string_agg(r."Name", ', ') FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON ur."RoleId" = r."Id" WHERE ur."UserId" = u."Id") as Roles,
    (SELECT COUNT(*) FROM "AspNetUserClaims" c WHERE c."UserId" = u."Id") as ClaimsCount,
    (SELECT string_agg(c."Type" || '=' || c."Value", ' | ') FROM "AspNetUserClaims" c WHERE c."UserId" = u."Id") as Claims
FROM "AspNetUsers" u
WHERE u."Email" IN ('aloticopty@tico.com', 'admin@qlservices.com')
ORDER BY u."Email";


