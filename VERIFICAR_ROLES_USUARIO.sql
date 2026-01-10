-- Script para verificar roles y usuarios en la base de datos
-- Ejecutar en PostgreSQL para diagnosticar problemas de acceso

-- 1. Verificar todos los roles
SELECT "Id", "Name", "NormalizedName" 
FROM "AspNetRoles" 
ORDER BY "Name";

-- 2. Verificar todos los usuarios
SELECT 
    u."Id",
    u."UserName",
    u."Email",
    u."FirstName",
    u."LastName",
    u."InstitutionId",
    u."IsActive",
    u."EmailConfirmed"
FROM "AspNetUsers" u
ORDER BY u."Email";

-- 3. Verificar roles asignados a usuarios
SELECT 
    u."UserName",
    u."Email",
    r."Name" as "Role",
    u."InstitutionId"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
ORDER BY u."Email", r."Name";

-- 4. Verificar específicamente el SuperAdmin
SELECT 
    u."Id",
    u."UserName",
    u."Email",
    u."FirstName",
    u."LastName",
    u."InstitutionId",
    u."IsActive",
    u."EmailConfirmed",
    r."Name" as "Role"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@qlservices.com';

-- 5. Verificar claims de InstitutionId
SELECT 
    u."UserName",
    u."Email",
    c."Type" as "ClaimType",
    c."Value" as "ClaimValue"
FROM "AspNetUsers" u
JOIN "AspNetUserClaims" c ON u."Id" = c."UserId"
WHERE c."Type" = 'InstitutionId'
ORDER BY u."Email";

-- 6. Verificar si SuperAdmin tiene InstitutionId (debe ser NULL)
SELECT 
    "Id",
    "UserName",
    "Email",
    "InstitutionId"
FROM "AspNetUsers"
WHERE "Email" = 'admin@qlservices.com';

-- 7. Si el SuperAdmin no tiene rol, agregarlo:
/*
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
        RAISE NOTICE 'Usuario o rol no encontrado';
    END IF;
END $$;
*/

