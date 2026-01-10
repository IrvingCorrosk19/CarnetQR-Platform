# Instrucciones de Impresión de Carnet - Foto y QR en Dos Caras

## ✅ Verificación de Funcionalidad

### 1. **Foto de la Entidad en el Carnet** ✅ IMPLEMENTADO

**Estado:** Ya está implementado y funcional

**Cómo activarlo:**
1. En la configuración de la institución (`/InstitutionConfig/CardSettings`):
   - Activar "Photo Enabled" (Habilitar foto)
2. Al crear o editar una entidad (paciente):
   - Subir una foto en el campo "PhotoFile"
3. Al imprimir el carnet:
   - La foto se mostrará automáticamente si:
     - La institución tiene `PhotoEnabled = true`
     - La entidad tiene una foto subida (`PhotoPath` no está vacío)
   - O usar parámetro: `?showPhoto=true`

**URL de impresión con foto:**
```
/Carnet/Print/{cardNumber}?showPhoto=true
```

### 2. **QR en la Parte Trasera** ✅ IMPLEMENTADO

**Estado:** Nueva funcionalidad implementada

**Cómo usar:**

#### Opción A: Via Query String (Simple)
```
/Carnet/Print/{cardNumber}?qrOnBack=true
```
Esto automáticamente:
- Activa impresión de dos caras (`doubleSided = true`)
- Mueve el QR a la parte trasera
- Oculta el QR del frente

#### Opción B: Configuración Completa
```
/Carnet/Print/{cardNumber}?doubleSided=true&qrOnBack=true&showPhoto=true
```

**Parámetros disponibles:**
- `doubleSided=true` - Activa impresión de dos caras
- `qrOnBack=true` - Mueve QR a la parte trasera (automáticamente activa doubleSided)
- `showPhoto=true` - Fuerza mostrar foto (si existe)
- `showLogo=true/false` - Mostrar/ocultar logo
- `showUserName=true/false` - Mostrar/ocultar nombre
- `showCardNumber=true/false` - Mostrar/ocultar número de carnet

### 3. **Configuración Recomendada**

Para tener **foto en el frente** y **QR en la trasera**:

```
/Carnet/Print/{cardNumber}?showPhoto=true&qrOnBack=true
```

**Resultado:**
- **FRENTE:** Logo, Nombre Institución, Foto, Nombre Usuario, Número Carnet, Detalles
- **TRASERA:** QR Code grande, Nombre Institución, Instrucciones, Número Carnet

## 📋 Diseño del Carnet

### Cara Frontal
```
┌─────────────────────────────────────┐
│ [LOGO]    NOMBRE INSTITUCIÓN       │
├─────────────────────────────────────┤
│ [FOTO]    NOMBRE COMPLETO           │
│ 25x30mm   Número: CARN-000001      │
│           ID: 123456789             │
│           Email: email@example.com  │
│                                     │
│           (NO QR si qrOnBack=true)  │
├─────────────────────────────────────┤
│ Emitido: DD/MM/YYYY                 │
└─────────────────────────────────────┘
```

### Cara Trasera (cuando qrOnBack=true)
```
┌─────────────────────────────────────┐
│                                     │
│         [QR CODE]                   │
│         35x35mm                     │
│                                     │
│      NOMBRE INSTITUCIÓN             │
│                                     │
│  Escanea el código QR para         │
│  verificar la información           │
│  del carnet                         │
│                                     │
│      Carnet: CARN-000001            │
│                                     │
└─────────────────────────────────────┘
```

## 🖨️ Instrucciones de Impresión

### Impresión de Dos Caras

1. **Vista Previa:**
   - La vista mostrará ambas caras una debajo de la otra
   - Un separador visual indica "PARTE TRASERA DEL CARNET"

2. **Configuración de Impresora:**
   - **Método 1 (Recomendado):** Configurar impresora para "Impresión a doble cara" automática
     - El navegador generará dos páginas separadas
     - La impresora manejará el volteo automáticamente
   - **Método 2 (Manual):**
     - Imprimir primero el frente
     - Voltear la hoja y volver a imprimir la trasera
     - Usar "Orientación del papel" en configuración de impresión

3. **Configuración de Página:**
   - Tamaño: 85.6mm x 54mm (formato tarjeta estándar)
   - Margen: 0mm (sin márgenes)
   - Orientación: Horizontal (por defecto)

## 🔍 Verificación Técnica

### Funcionalidad Verificada:

✅ **Foto en frente:**
- Se muestra cuando `PhotoEnabled = true` en institución
- Se muestra cuando `PhotoPath` existe en entidad
- Se puede forzar con `?showPhoto=true`

✅ **QR en trasera:**
- Se mueve a trasera con `?qrOnBack=true`
- Automáticamente activa dos caras
- Se oculta del frente cuando está en trasera
- Se muestra grande (35x35mm) en el centro de la trasera

✅ **Dos caras:**
- Soporte completo para impresión de dos caras
- Cada cara en página separada
- CSS optimizado para impresión
- Vista previa muestra ambas caras

## 📝 Ejemplos de Uso

### Ejemplo 1: Carnet con foto y QR en frente (actual)
```
/Carnet/Print/HEMO-000001
```

### Ejemplo 2: Carnet con foto en frente y QR en trasera (nuevo)
```
/Carnet/Print/HEMO-000001?showPhoto=true&qrOnBack=true
```

### Ejemplo 3: Solo QR en trasera (sin foto)
```
/Carnet/Print/HEMO-000001?qrOnBack=true
```

### Ejemplo 4: Personalización completa
```
/Carnet/Print/HEMO-000001?showPhoto=true&qrOnBack=true&showLogo=true&showEmail=true
```

## ⚠️ Notas Importantes

1. **Foto requerida:** Para mostrar foto, debe:
   - Estar habilitada en la configuración de la institución
   - Existir en el EntityProfile (subida al crear/editar)

2. **Impresión de dos caras:**
   - Funciona mejor con impresoras que soportan impresión a doble cara automática
   - En impresoras sin soporte, se debe hacer manualmente (imprimir frente, voltear, imprimir trasera)

3. **Tamaño estándar:**
   - 85.6mm x 54mm (tamaño tarjeta ISO 7810)
   - Se puede personalizar con parámetros `width` y `height`

## 🎨 Personalización Adicional

El código soporta personalización vía query string:
- `width=85.6` - Ancho en mm
- `height=54.0` - Alto en mm
- `orientation=horizontal|vertical` - Orientación
- Todos los elementos `show*` pueden controlarse individualmente

