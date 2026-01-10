# Verificación: Foto en Frente y QR en Trasera del Carnet

## ✅ VERIFICACIÓN COMPLETADA

### 1. **Foto de la Entidad en el Carnet** ✅ FUNCIONAL

**Estado:** ✅ Ya está implementado y funcionando

**Implementación verificada:**
- ✅ `PhotoPath` existe en `EntityProfile` (línea 17 de PrintCardViewModel)
- ✅ Se carga correctamente: `PhotoPath = card.EntityProfile?.PhotoPath` (línea 64 del controlador)
- ✅ Se verifica configuración: `card.Institution?.PhotoEnabled == true` (línea 52 del controlador)
- ✅ Se muestra en vista si: `config.ShowPhoto && !string.IsNullOrEmpty(Model.PhotoPath)` (línea 282 de la vista)
- ✅ Configuración por defecto: `ShowPhoto = photoEnabled` (línea 90 del controlador)

**Cómo usar:**
1. Configurar institución: `/InstitutionConfig/CardSettings` → Activar "Photo Enabled"
2. Subir foto al crear/editar entidad (paciente)
3. Imprimir carnet → La foto aparecerá automáticamente

**URL de ejemplo:**
```
/Carnet/Print/HEMO-000001?showPhoto=true
```

### 2. **QR en la Parte Trasera** ✅ IMPLEMENTADO

**Estado:** ✅ Nueva funcionalidad implementada correctamente

**Implementación verificada:**
- ✅ `QrOnBack` agregado a `PrintCardConfig` (línea 35 del modelo)
- ✅ `DoubleSided` agregado para soporte de dos caras (línea 34 del modelo)
- ✅ Lógica en controlador: Activa automáticamente `DoubleSided` cuando `qrOnBack=true` (líneas 184-188)
- ✅ Vista trasera implementada: Se muestra cuando `config.DoubleSided = true` (línea 444 de la vista)
- ✅ QR se oculta del frente cuando `qrOnBack=true` (línea 428: `!config.QrOnBack`)
- ✅ QR se muestra en trasera cuando `config.QrOnBack = true` (línea 454 de la vista)
- ✅ CSS para impresión de dos caras con `page-break-after: always` (línea 226, 305-314)

**Cómo usar:**
```
/Carnet/Print/HEMO-000001?qrOnBack=true
```

Esto automáticamente:
- ✅ Activa impresión de dos caras (`DoubleSided = true`)
- ✅ Mueve QR a la parte trasera
- ✅ Oculta QR del frente

## 📋 Diseño Verificado

### Cara Frontal (cuando `qrOnBack=true` y `showPhoto=true`)
```
┌─────────────────────────────────────┐
│ [LOGO]    NOMBRE INSTITUCIÓN       │
├─────────────────────────────────────┤
│ [FOTO]    NOMBRE COMPLETO           │
│ 25x30mm   Número: CARN-000001      │
│           ID: 123456789             │
│           Email: email@example.com  │
│                                     │
│           (SIN QR - va en trasera)   │
├─────────────────────────────────────┤
│ Emitido: DD/MM/YYYY                 │
└─────────────────────────────────────┘
```

### Cara Trasera (cuando `qrOnBack=true`)
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

## 🔍 Verificación Técnica Detallada

### Código Verificado:

1. **PrintCardViewModel.cs:**
   - ✅ `PhotoPath` existe y se asigna
   - ✅ `QrOnBack` agregado a `PrintCardConfig`
   - ✅ `DoubleSided` agregado para dos caras

2. **CarnetController.cs:**
   - ✅ `photoEnabled` calculado correctamente (línea 52)
   - ✅ `PhotoPath` asignado al viewModel (línea 64)
   - ✅ `ShowPhoto` configurado por defecto (línea 90)
   - ✅ Parámetros `qrOnBack` y `doubleSided` procesados (líneas 170-190)
   - ✅ Lógica: Si `qrOnBack=true`, automáticamente activa `DoubleSided` y oculta QR del frente (líneas 184-188)

3. **PrintCarnet.cshtml:**
   - ✅ Foto se muestra si `config.ShowPhoto && !string.IsNullOrEmpty(Model.PhotoPath)` (línea 381)
   - ✅ QR se oculta del frente si `config.QrOnBack` es true (línea 428: `!config.QrOnBack`)
   - ✅ Vista trasera se muestra si `config.DoubleSided` es true (línea 444)
   - ✅ QR se muestra en trasera si `config.QrOnBack && !string.IsNullOrEmpty(Model.QrCodeBase64)` (línea 454)
   - ✅ CSS para impresión de dos caras con `page-break-after: always` (líneas 305-314)

### Flujo de Funcionamiento:

1. **Usuario imprime con:** `/Carnet/Print/{cardNumber}?showPhoto=true&qrOnBack=true`

2. **Controlador procesa:**
   - Verifica `PhotoEnabled` en institución
   - Verifica `PhotoPath` en entidad
   - Si `qrOnBack=true`:
     - Activa `DoubleSided = true`
     - Configura `ShowQrCode = false` (oculta del frente)

3. **Vista renderiza:**
   - **Frente:** Logo, Foto, Nombre, Datos (SIN QR)
   - **Trasera:** QR grande centrado, Institución, Instrucciones

4. **CSS para impresión:**
   - Cada cara en página separada (`page-break-after: always`)
   - Tamaño correcto (85.6mm x 54mm)

## ✅ CONCLUSIÓN

**SÍ, SE PUEDE HACER:**

1. ✅ **Foto de la entidad en el frente:** Ya está implementado y funcional
   - Requiere: `PhotoEnabled = true` en institución + foto subida en entidad
   - Se puede forzar con `?showPhoto=true`

2. ✅ **QR en la parte trasera:** Implementado y funcional
   - Se activa con `?qrOnBack=true`
   - Automáticamente activa dos caras
   - Mueve QR del frente a la trasera
   - Muestra QR grande (35x35mm) centrado en la trasera

## 📝 Ejemplos de Uso

### Ejemplo 1: Carnet completo (Foto en frente, QR en trasera) ⭐ RECOMENDADO
```
/Carnet/Print/HEMO-000001?showPhoto=true&qrOnBack=true
```

### Ejemplo 2: Solo QR en trasera (sin foto)
```
/Carnet/Print/HEMO-000001?qrOnBack=true
```

### Ejemplo 3: Foto y QR en frente (configuración actual por defecto)
```
/Carnet/Print/HEMO-000001?showPhoto=true
```

## 🖨️ Instrucciones de Impresión

1. **Impresora con soporte de dos caras automático:**
   - Configurar impresora para "Impresión a doble cara"
   - El navegador generará dos páginas separadas
   - La impresora manejará el volteo automáticamente

2. **Impresora sin soporte de dos caras:**
   - Imprimir primero el frente (página 1)
   - Voltear la hoja manualmente
   - Imprimir nuevamente la trasera (página 2)

## ⚠️ Notas Importantes

1. **Para mostrar foto:**
   - Debe estar habilitada en `/InstitutionConfig/CardSettings` (PhotoEnabled = true)
   - La entidad debe tener una foto subida (PhotoPath no vacío)

2. **Para QR en trasera:**
   - Usar parámetro `?qrOnBack=true`
   - Automáticamente activa dos caras
   - El QR no se mostrará en el frente

3. **Vista previa:**
   - En pantalla se muestran ambas caras una debajo de la otra
   - Separador visual indica "PARTE TRASERA DEL CARNET"
   - En impresión, cada cara va en su propia página

