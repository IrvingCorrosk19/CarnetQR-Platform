using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CarnetQRPlatform.Web.Scripts;

public class ConsultarUsuarios
{
    public static async Task Main(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=carnetqr_platform_db;Username=postgres;Password=Panama2020$";
        
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        Console.WriteLine("=== COMPARACIÓN DE USUARIOS ===\n");
        
        // Consulta 1: Información básica
        var query1 = @"
            SELECT 
                u.""Email"",
                u.""UserName"",
                u.""NormalizedEmail"",
                u.""NormalizedUserName"",
                u.""EmailConfirmed"",
                u.""IsActive"",
                u.""InstitutionId"",
                u.""FirstName"",
                u.""LastName"",
                u.""LockoutEnabled"",
                u.""LockoutEnd"",
                u.""AccessFailedCount"",
                CASE 
                    WHEN u.""PasswordHash"" IS NULL THEN 'SIN CONTRASEÑA'
                    ELSE 'TIENE CONTRASEÑA'
                END as PasswordStatus
            FROM ""AspNetUsers"" u
            WHERE u.""Email"" IN ('aloticopty@tico.com', 'admin@qlservices.com')
            ORDER BY u.""Email"";
        ";
        
        Console.WriteLine("1. INFORMACIÓN BÁSICA:");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        await using (var cmd = new NpgsqlCommand(query1, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"Email: {reader["Email"]}");
                Console.WriteLine($"UserName: {reader["UserName"]}");
                Console.WriteLine($"EmailConfirmed: {reader["EmailConfirmed"]}");
                Console.WriteLine($"IsActive: {reader["IsActive"]}");
                Console.WriteLine($"InstitutionId: {reader["InstitutionId"]?.ToString() ?? "NULL"}");
                Console.WriteLine($"LockoutEnd: {reader["LockoutEnd"]?.ToString() ?? "NULL"}");
                Console.WriteLine($"AccessFailedCount: {reader["AccessFailedCount"]}");
                Console.WriteLine($"PasswordStatus: {reader["PasswordStatus"]}");
                Console.WriteLine();
            }
        }
        
        // Consulta 2: Roles
        var query2 = @"
            SELECT 
                u.""Email"",
                r.""Name"" as RoleName
            FROM ""AspNetUsers"" u
            LEFT JOIN ""AspNetUserRoles"" ur ON u.""Id"" = ur.""UserId""
            LEFT JOIN ""AspNetRoles"" r ON ur.""RoleId"" = r.""Id""
            WHERE u.""Email"" IN ('aloticopty@tico.com', 'admin@qlservices.com')
            ORDER BY u.""Email"", r.""Name"";
        ";
        
        Console.WriteLine("2. ROLES ASIGNADOS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        await using (var cmd = new NpgsqlCommand(query2, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            string currentEmail = "";
            while (await reader.ReadAsync())
            {
                var email = reader["Email"]?.ToString() ?? "";
                if (email != currentEmail)
                {
                    Console.WriteLine($"\n{email}:");
                    currentEmail = email;
                }
                var role = reader["RoleName"]?.ToString() ?? "SIN ROL";
                Console.WriteLine($"  - {role}");
            }
        }
        Console.WriteLine();
        
        // Consulta 3: Claims
        var query3 = @"
            SELECT 
                u.""Email"",
                c.""Type"" as ClaimType,
                c.""Value"" as ClaimValue
            FROM ""AspNetUsers"" u
            LEFT JOIN ""AspNetUserClaims"" c ON u.""Id"" = c.""UserId""
            WHERE u.""Email"" IN ('aloticopty@tico.com', 'admin@qlservices.com')
            ORDER BY u.""Email"", c.""Type"";
        ";
        
        Console.WriteLine("3. CLAIMS ASIGNADOS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        await using (var cmd = new NpgsqlCommand(query3, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            string currentEmail = "";
            while (await reader.ReadAsync())
            {
                var email = reader["Email"]?.ToString() ?? "";
                if (email != currentEmail)
                {
                    Console.WriteLine($"\n{email}:");
                    currentEmail = email;
                }
                var claimType = reader["ClaimType"]?.ToString();
                var claimValue = reader["ClaimValue"]?.ToString();
                if (claimType != null)
                {
                    Console.WriteLine($"  - {claimType} = {claimValue}");
                }
                else
                {
                    Console.WriteLine("  - SIN CLAIMS");
                }
            }
        }
        Console.WriteLine();
        
        // Consulta 4: Instituciones
        var query4 = @"
            SELECT 
                u.""Email"",
                u.""InstitutionId"",
                i.""Name"" as InstitutionName,
                i.""IsActive"" as InstitutionIsActive
            FROM ""AspNetUsers"" u
            LEFT JOIN ""Institutions"" i ON u.""InstitutionId"" = i.""Id""
            WHERE u.""Email"" IN ('aloticopty@tico.com', 'admin@qlservices.com')
            ORDER BY u.""Email"";
        ";
        
        Console.WriteLine("4. INSTITUCIONES ASOCIADAS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        await using (var cmd = new NpgsqlCommand(query4, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"Email: {reader["Email"]}");
                Console.WriteLine($"InstitutionId: {reader["InstitutionId"]?.ToString() ?? "NULL"}");
                Console.WriteLine($"InstitutionName: {reader["InstitutionName"]?.ToString() ?? "N/A"}");
                Console.WriteLine($"InstitutionIsActive: {reader["InstitutionIsActive"]?.ToString() ?? "N/A"}");
                Console.WriteLine();
            }
        }
        
        // Consulta 5: Análisis de problemas
        var query5 = @"
            SELECT 
                u.""Email"",
                CASE 
                    WHEN u.""PasswordHash"" IS NULL THEN 'PROBLEMA: No tiene contraseña'
                    ELSE 'OK: Tiene contraseña'
                END as PasswordCheck,
                CASE 
                    WHEN u.""EmailConfirmed"" = false THEN 'PROBLEMA: Email no confirmado'
                    ELSE 'OK: Email confirmado'
                END as EmailCheck,
                CASE 
                    WHEN u.""IsActive"" = false THEN 'PROBLEMA: Usuario inactivo'
                    ELSE 'OK: Usuario activo'
                END as ActiveCheck,
                CASE 
                    WHEN u.""LockoutEnd"" IS NOT NULL AND u.""LockoutEnd"" > NOW() THEN 'PROBLEMA: Usuario bloqueado'
                    WHEN u.""AccessFailedCount"" >= 5 THEN 'ADVERTENCIA: ' || u.""AccessFailedCount"" || ' intentos fallidos'
                    ELSE 'OK: No bloqueado'
                END as LockoutCheck,
                CASE 
                    WHEN NOT EXISTS (SELECT 1 FROM ""AspNetUserRoles"" ur WHERE ur.""UserId"" = u.""Id"") THEN 'PROBLEMA: No tiene roles'
                    ELSE 'OK: Tiene roles'
                END as RoleCheck,
                CASE 
                    WHEN u.""InstitutionId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ""Institutions"" i WHERE i.""Id"" = u.""InstitutionId"") THEN 'PROBLEMA: InstitutionId no existe'
                    ELSE 'OK: Institución válida o SuperAdmin'
                END as InstitutionCheck
            FROM ""AspNetUsers"" u
            WHERE u.""Email"" IN ('aloticopty@tico.com', 'admin@qlservices.com')
            ORDER BY u.""Email"";
        ";
        
        Console.WriteLine("5. ANÁLISIS DE PROBLEMAS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        await using (var cmd = new NpgsqlCommand(query5, connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"\nUsuario: {reader["Email"]}");
                Console.WriteLine($"  Password: {reader["PasswordCheck"]}");
                Console.WriteLine($"  Email: {reader["EmailCheck"]}");
                Console.WriteLine($"  Activo: {reader["ActiveCheck"]}");
                Console.WriteLine($"  Bloqueo: {reader["LockoutCheck"]}");
                Console.WriteLine($"  Roles: {reader["RoleCheck"]}");
                Console.WriteLine($"  Institución: {reader["InstitutionCheck"]}");
            }
        }
        
        Console.WriteLine("\n=== FIN DEL REPORTE ===");
    }
}


