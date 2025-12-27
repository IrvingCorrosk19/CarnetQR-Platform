# CarnetQR Platform

## Verificar Usuarios Creados

Para verificar que los usuarios se hayan insertado correctamente en la base de datos, puedes:

1. **Usar el endpoint de prueba** (solo desarrollo):
   ```
   GET /Test/CheckUsers
   ```
   Esto mostrará todos los usuarios, roles e instituciones en formato JSON.

2. **Ejecutar la aplicación**:
   ```bash
   dotnet run --project CarnetQRPlatform.Web
   ```
   El DbInitializer se ejecutará automáticamente al iniciar y creará los usuarios si no existen.

## Credenciales de Acceso

- **SuperAdmin**: admin@qlservices.com / Admin@123456
- **Demo Admin**: admin@demo.com / Admin@123456

## Base de Datos

La aplicación usa PostgreSQL en:
- Host: localhost
- Port: 5432
- Database: carnetqr_platform_db
- Username: postgres
- Password: Panama2020$

