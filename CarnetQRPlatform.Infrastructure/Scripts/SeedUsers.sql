-- Script para insertar usuarios iniciales
-- Ejecutar después de las migraciones

-- Insertar roles si no existen
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid()::text, 'SuperAdmin', 'SUPERADMIN', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'SUPERADMIN');

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid()::text, 'InstitutionAdmin', 'INSTITUTIONADMIN', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'INSTITUTIONADMIN');

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid()::text, 'Staff', 'STAFF', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'STAFF');

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid()::text, 'AdministrativeOperator', 'ADMINISTRATIVEOPERATOR', gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'ADMINISTRATIVEOPERATOR');

-- Insertar SuperAdmin (usuario)
-- Password: Admin@123456 (hasheado con ASP.NET Identity)
DO $$
DECLARE
    user_id TEXT := gen_random_uuid()::text;
    role_id TEXT;
BEGIN
    -- Verificar si el usuario ya existe
    IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'admin@qlservices.com') THEN
        -- Insertar usuario
        INSERT INTO "AspNetUsers" (
            "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", 
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
            "FirstName", "LastName", "InstitutionId", "IsActive"
        ) VALUES (
            user_id,
            'admin@qlservices.com',
            'ADMIN@QLSERVICES.COM',
            'admin@qlservices.com',
            'ADMIN@QLSERVICES.COM',
            true,
            'AQAAAAIAAYagAAAAENfGfKvB1JHQjX+o9PdJhxp/kqF1Td1cP3xqVJYq4rN8XQJYqP4kL5M6N7O8P9Q0R1S2T3U4V5W6X7Y8Z9A0B1C2D3E4F5G6H7I8J9K0L1M2N3O4P5Q6R7S8T9U0V1W2X3Y4Z5',
            gen_random_uuid()::text,
            gen_random_uuid()::text,
            false,
            false,
            false,
            0,
            'Super',
            'Admin',
            '00000000-0000-0000-0000-000000000000'::uuid,
            true
        );

        -- Obtener role_id
        SELECT "Id" INTO role_id FROM "AspNetRoles" WHERE "NormalizedName" = 'SUPERADMIN';

        -- Asignar rol
        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
        VALUES (user_id, role_id)
        ON CONFLICT DO NOTHING;
    END IF;
END $$;

