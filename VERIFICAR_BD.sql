-- ============================================
-- Script SQL para Verificar Carnets en BD
-- CarnetQR Platform
-- ============================================

-- 1. Ver todos los carnets con información completa
SELECT 
    c."Id" as "CardId",
    c."CardNumber" as "NumeroCarnet",
    c."QrToken" as "TokenQR",
    c."IssuedAt" as "FechaEmision",
    c."IsActive" as "Activo",
    i."Name" as "Empresa",
    i."CardPrefix" as "PrefijoEmpresa",
    ep."IdentificationNumber" as "CedulaEntidad",
    ep."FirstName" || ' ' || ep."LastName" as "NombreEntidad",
    ep."Email" as "EmailEntidad"
FROM "Cards" c
INNER JOIN "Institutions" i ON c."InstitutionId" = i."Id"
INNER JOIN "EntityProfiles" ep ON c."EntityProfileId" = ep."Id"
ORDER BY c."IssuedAt" DESC;

-- 2. Contar carnets por empresa
SELECT 
    i."Name" as "Empresa",
    COUNT(c."Id") as "TotalCarnets",
    COUNT(CASE WHEN c."IsActive" = true THEN 1 END) as "CarnetsActivos",
    COUNT(CASE WHEN c."IsActive" = false THEN 1 END) as "CarnetsInactivos"
FROM "Institutions" i
LEFT JOIN "Cards" c ON i."Id" = c."InstitutionId"
GROUP BY i."Id", i."Name"
ORDER BY "TotalCarnets" DESC;

-- 3. Ver carnets de una empresa específica (reemplaza 'TU-INSTITUTION-ID')
SELECT 
    c."CardNumber",
    c."QrToken",
    c."IssuedAt",
    ep."FirstName" || ' ' || ep."LastName" as "Entidad"
FROM "Cards" c
INNER JOIN "EntityProfiles" ep ON c."EntityProfileId" = ep."Id"
WHERE c."InstitutionId" = 'TU-INSTITUTION-ID-AQUI'
ORDER BY c."IssuedAt" DESC;

-- 4. Ver carnets de una entidad específica (reemplaza 'TU-ENTITY-ID')
SELECT 
    c."CardNumber",
    c."QrToken",
    c."IssuedAt",
    c."IsActive"
FROM "Cards" c
WHERE c."EntityProfileId" = 'TU-ENTITY-ID-AQUI'
ORDER BY c."IssuedAt" DESC;

-- 5. Verificar integridad: Entidades sin carnets
SELECT 
    ep."Id",
    ep."FirstName" || ' ' || ep."LastName" as "Nombre",
    ep."IdentificationNumber" as "Cedula",
    i."Name" as "Empresa"
FROM "EntityProfiles" ep
INNER JOIN "Institutions" i ON ep."InstitutionId" = i."Id"
LEFT JOIN "Cards" c ON ep."Id" = c."EntityProfileId"
WHERE c."Id" IS NULL
ORDER BY ep."CreatedAt" DESC;

-- 6. Verificar integridad: Carnets huérfanos (sin entidad)
SELECT 
    c."Id",
    c."CardNumber",
    c."QrToken"
FROM "Cards" c
LEFT JOIN "EntityProfiles" ep ON c."EntityProfileId" = ep."Id"
WHERE ep."Id" IS NULL;

-- 7. Verificar integridad: Carnets huérfanos (sin empresa)
SELECT 
    c."Id",
    c."CardNumber",
    c."QrToken"
FROM "Cards" c
LEFT JOIN "Institutions" i ON c."InstitutionId" = i."Id"
WHERE i."Id" IS NULL;

-- 8. Últimos 10 carnets creados
SELECT 
    c."CardNumber",
    ep."FirstName" || ' ' || ep."LastName" as "Entidad",
    i."Name" as "Empresa",
    c."IssuedAt"
FROM "Cards" c
INNER JOIN "EntityProfiles" ep ON c."EntityProfileId" = ep."Id"
INNER JOIN "Institutions" i ON c."InstitutionId" = i."Id"
ORDER BY c."IssuedAt" DESC
LIMIT 10;

-- 9. Estadísticas de carnets por mes
SELECT 
    DATE_TRUNC('month', c."IssuedAt") as "Mes",
    COUNT(c."Id") as "CarnetsGenerados"
FROM "Cards" c
GROUP BY DATE_TRUNC('month', c."IssuedAt")
ORDER BY "Mes" DESC;

-- 10. Verificar duplicados de CardNumber (no debería haber)
SELECT 
    "CardNumber",
    COUNT(*) as "Cantidad"
FROM "Cards"
GROUP BY "CardNumber"
HAVING COUNT(*) > 1;

-- 11. Verificar duplicados de QrToken (no debería haber)
SELECT 
    "QrToken",
    COUNT(*) as "Cantidad"
FROM "Cards"
GROUP BY "QrToken"
HAVING COUNT(*) > 1;

