# Guía: Cómo Crear un Carnet QR

## Flujo Completo de Creación de Carnet

### Paso 1: Tener una Entidad Creada
- Debes tener al menos una entidad (EntityProfile) creada en el sistema
- La entidad debe estar activa
- La entidad debe pertenecer a una empresa (Institution)

### Paso 2: Acceder a los Detalles de la Entidad
1. Ve a **Entidades** en el menú
2. Busca la entidad que deseas
3. Haz clic en el botón **"Ver"** (ícono de ojo) o en el nombre de la entidad

### Paso 3: Generar el Carnet
1. En la página de detalles de la entidad, verás:
   - Información de la entidad
   - Sección "Carnets Asociados" (inicialmente vacía)
   - Botón **"Generar Carnet"** (verde)

2. Haz clic en **"Generar Carnet"**
3. El sistema:
   - Valida que la entidad pertenezca a tu empresa (multi-tenant)
   - Obtiene el prefijo de la empresa (ej: "DEMO", "MF")
   - Genera un número de carnet único: `PREFIJO + 6 dígitos` (ej: "DEMO000001")
   - Genera un token QR seguro y único
   - Guarda el carnet en la base de datos
   - Asocia el carnet a la entidad

### Paso 4: Ver el Carnet Creado
- La página se recarga automáticamente
- Verás el nuevo carnet en la tabla "Carnets Asociados"
- Puedes hacer clic en el carnet para ver detalles completos

## Estructura del Carnet en la Base de Datos

### Tabla: `Cards`

```sql
- Id (Guid) - Identificador único
- InstitutionId (Guid) - Empresa a la que pertenece
- EntityProfileId (Guid) - Entidad asociada
- CardNumber (string) - Número del carnet (ej: "DEMO000001")
- QrToken (string) - Token seguro para el QR (32 caracteres)
- IssuedAt (DateTime) - Fecha de emisión
- ExpiresAt (DateTime?) - Fecha de expiración (opcional)
- IsActive (bool) - Estado activo/inactivo
- CreatedAt (DateTime) - Fecha de creación
- UpdatedAt (DateTime?) - Fecha de última actualización
```

### Relaciones:
- `Card.InstitutionId` → `Institution.Id`
- `Card.EntityProfileId` → `EntityProfile.Id`

## Verificación en Base de Datos

### Consulta SQL para verificar carnets:

```sql
-- Ver todos los carnets
SELECT 
    c."Id",
    c."CardNumber",
    c."QrToken",
    c."IssuedAt",
    c."IsActive",
    i."Name" as "InstitutionName",
    ep."FirstName" || ' ' || ep."LastName" as "EntityName"
FROM "Cards" c
INNER JOIN "Institutions" i ON c."InstitutionId" = i."Id"
INNER JOIN "EntityProfiles" ep ON c."EntityProfileId" = ep."Id"
ORDER BY c."IssuedAt" DESC;

-- Ver carnets de una empresa específica
SELECT * FROM "Cards" 
WHERE "InstitutionId" = 'TU-INSTITUTION-ID-AQUI';

-- Ver carnets de una entidad específica
SELECT * FROM "Cards" 
WHERE "EntityProfileId" = 'TU-ENTITY-ID-AQUI';
```

## URL del QR Público

Una vez creado el carnet, puedes acceder al QR público mediante:

```
https://tu-dominio.com/q/{QrToken}
```

Ejemplo:
```
https://localhost:7003/q/abc123def456ghi789jkl012mno345pq
```

## Logging para Debug

El sistema tiene logging completo en:
- **Backend (Console)**: Verás logs en la consola donde ejecutas la aplicación
- **Frontend (Browser Console)**: Verás logs en F12 → Console

Los logs muestran:
- EntityProfileId recibido
- TenantId detectado
- Validaciones realizadas
- Número de carnet generado
- Token QR generado
- Confirmación de guardado en BD

## Problemas Comunes

### Error: "EntityProfile not found or access denied"
- **Causa**: La entidad no existe o pertenece a otra empresa
- **Solución**: Verifica que la entidad pertenezca a tu empresa

### Error: "Institution not found"
- **Causa**: La empresa de la entidad no existe
- **Solución**: Verifica que la empresa esté activa

### Error: "Cannot create card without tenant context"
- **Causa**: El usuario no tiene empresa asignada (solo SuperAdmin puede crear sin tenant)
- **Solución**: Asigna una empresa al usuario o usa SuperAdmin

## Validaciones Aplicadas

1. ✅ Multi-tenant: Solo puedes crear carnets para entidades de tu empresa
2. ✅ Unicidad: CardNumber y QrToken son únicos en todo el sistema
3. ✅ Numeración: Los números se generan secuencialmente por empresa
4. ✅ Seguridad: El token QR es criptográficamente seguro

