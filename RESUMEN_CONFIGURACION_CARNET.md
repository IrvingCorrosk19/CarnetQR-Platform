# Resumen: Sistema de Configuración Avanzada del Carnet

## ✅ Mejoras Implementadas

### 1. **PrintCardConfig Extendido**

Se ha extendido significativamente la clase `PrintCardConfig` con más de **50 propiedades configurables**, organizadas en las siguientes categorías:

#### A. **Tamaño y Orientación**
- `Width`, `Height` (en mm)
- `Orientation` (horizontal/vertical)

#### B. **Dos Caras**
- `DoubleSided` - Impresión de dos caras
- `QrOnBack` - QR en la parte trasera
- `BackRotate180` - Rotación de 180° para impresoras de dos caras

#### C. **Colores y Estilos**
- `PrimaryColor`, `SecondaryColor`
- `BackgroundColor`, `BackgroundGradient`
- `TextColor`, `BorderColor`
- `BorderStyle` (solid, dashed, dotted, double)
- `BorderWidth`, `BorderRadius`

#### D. **Fuentes**
- `FontFamily` - Familia de fuente personalizable
- `FontSizeName`, `FontSizeCardNumber`, `FontSizeDetails` (en pt)
- `FontWeightName`, `FontWeightDetails` (400=normal, 700=bold)

#### E. **Tamaños de Elementos**
- `PhotoWidth`, `PhotoHeight` (mm)
- `QrSize` (frente, mm)
- `QrBackSize` (trasera, mm)
- `LogoWidth`, `LogoHeight` (mm)

#### F. **Posicionamiento**
- `LogoPosition` (top-left, top-right, top-center, etc.)
- `PhotoPosition` (left, right, top, center)
- `QrPosition` (top-left, top-right, bottom-left, bottom-right, center, left, right)
- `QrBackPosition` (center, top, bottom, left, right)
- `TextAlignment` (left, center, right, justify)

#### G. **Layouts Predefinidos**
- `LayoutStyle` (standard, compact, expanded, professional, simple)

#### H. **Elementos Visibles**
- `ShowLogo`, `ShowInstitutionName`, `ShowUserName`
- `ShowCardNumber`, `ShowQrCode`, `ShowPhoto`
- `ShowIdentificationNumber`, `ShowEmail`, `ShowPhone`
- `ShowDateOfBirth`, `ShowIssuedDate`

#### I. **Espaciado y Márgenes**
- `Padding` - Padding interno del carnet (mm)
- `SpacingBetweenElements` - Espacio entre elementos (mm)
- `MarginTop`, `MarginBottom`, `MarginLeft`, `MarginRight` (mm)

#### J. **Configuración de Trasera**
- `BackContent` (qr, info, custom)
- `BackTextAlignment`, `BackBackgroundColor`
- `BackInstructions` - Instrucciones personalizadas
- `BackShowInstitutionName`, `BackShowCardNumber`, `BackShowContactInfo`

#### K. **Efectos Visuales**
- `ShowShadow`, `ShadowOpacity`
- `ShowGradient` - Mostrar gradiente de fondo
- `Watermark`, `WatermarkOpacity`, `WatermarkPosition`

#### L. **Campos Personalizados**
- `CustomFields` - Dictionary de campos personalizados
- `CustomFieldsOrder` - Orden de campos personalizados

#### M. **Formatos**
- `DateFormat` - Formato de fecha (ej: "dd/MM/yyyy")
- `TimeFormat` - Formato de hora
- `FooterText` - Texto personalizado en footer
- `BackContactInfo` - Info de contacto para trasera

#### N. **Configuración de Impresión**
- `PrintResolution` (150dpi, 300dpi, 600dpi)
- `ColorMode` (RGB, CMYK)
- `OptimizeForPrint` - Optimizaciones para impresión

### 2. **Integración con CardTemplate**

Se ha integrado el sistema de templates existente (`CardTemplate`) en el proceso de impresión:

- **Carga automática del template por defecto** de la institución al imprimir
- **Sobrescritura por template específico** vía query string: `?templateId={guid}`
- **Mapeo automático** de `TemplateConfig` (Dictionary) a `PrintCardConfig`
- **Conversión inteligente** de tipos de datos

### 3. **Aplicación de Configuraciones**

El sistema aplica configuraciones en el siguiente orden de prioridad:

1. **Valores por defecto** de `PrintCardConfig`
2. **Configuración del template** (si existe)
3. **Configuración de la institución** (`VisibleFields`, `PhotoEnabled`)
4. **Sobrescritura vía query string** (mayor prioridad)

### 4. **Query String Parameters**

Ahora se puede personalizar el carnet completamente vía query string:

#### Ejemplos de Uso:

```bash
# Tamaño y orientación
/Carnet/Print/CARD001?width=90&height=55&orientation=vertical

# Colores personalizados
/Carnet/Print/CARD001?primaryColor=%23FF5733&secondaryColor=%23C70039&backgroundColor=%23FFFFFF

# Tamaños de elementos
/Carnet/Print/CARD001?qrSize=30&photoWidth=28&photoHeight=35

# Posicionamiento
/Carnet/Print/CARD001?photoPosition=right&qrPosition=top-right&logoPosition=top-center&textAlignment=center

# Layouts
/Carnet/Print/CARD001?layoutStyle=professional

# Elementos visibles
/Carnet/Print/CARD001?showEmail=true&showPhone=true&showIdentificationNumber=true

# Dos caras con QR en la trasera
/Carnet/Print/CARD001?doubleSided=true&qrOnBack=true&backRotate180=true

# Template específico
/Carnet/Print/CARD001?templateId=550e8400-e29b-41d4-a716-446655440000

# Combinación completa
/Carnet/Print/CARD001?templateId=550e8400-e29b-41d4-a716-446655440000&qrSize=32&primaryColor=%23667eea&showEmail=true
```

### 5. **Métodos Helper Implementados**

#### `ApplyTemplateConfig`
Aplica configuraciones desde `TemplateConfig` (Dictionary) a `PrintCardConfig` usando reflection y conversión de tipos.

#### `ApplyInstitutionVisibleFields`
Aplica campos visibles desde la configuración de institución a `PrintCardConfig`.

#### `ApplyQueryStringOverrides`
Aplica sobrescrituras desde query string, permitiendo personalización rápida sin modificar templates.

#### `ConvertValue`
Convierte valores de diferentes tipos (double, bool, string, Dictionary, List) a los tipos esperados por `PrintCardConfig`.

## 📋 Cómo Usar las Nuevas Configuraciones

### Opción 1: Via Query String (Rápido)

Para personalizaciones rápidas sin modificar templates:

```
/Carnet/Print/CARD001?primaryColor=%23667eea&qrSize=30&layoutStyle=professional
```

### Opción 2: Via CardTemplate (Recomendado)

Para configuraciones persistentes que se aplican a todos los carnets:

1. Crear un `CardTemplate` con `TemplateConfig`:
```json
{
  "primaryColor": "#667eea",
  "secondaryColor": "#764ba2",
  "qrSize": 28.0,
  "photoPosition": "left",
  "layoutStyle": "professional",
  "showEmail": true,
  "showPhone": false,
  "fontSizeName": 14.0,
  "fontWeightName": "700",
  "borderRadius": 12.0
}
```

2. Guardar el template en la base de datos
3. Marcar como `IsDefault = true` si debe aplicarse por defecto
4. Al imprimir, se aplicará automáticamente

### Opción 3: Via Institución (Configuración Global)

Usar `Institution.VisibleFields` para controlar qué campos mostrar en todos los carnets de la institución.

## 🎨 Templates Predefinidos Sugeridos

A continuación se sugieren algunos templates predefinidos que se pueden crear:

### Template "Profesional"
```json
{
  "primaryColor": "#2c3e50",
  "secondaryColor": "#34495e",
  "backgroundColor": "#ffffff",
  "textColor": "#2c3e50",
  "photoPosition": "left",
  "qrPosition": "right",
  "qrSize": 30.0,
  "layoutStyle": "professional",
  "fontSizeName": 14.0,
  "fontWeightName": "700",
  "showShadow": true,
  "showGradient": false,
  "qrOnBack": true,
  "doubleSided": true
}
```

### Template "Simple"
```json
{
  "primaryColor": "#333333",
  "backgroundColor": "#ffffff",
  "textColor": "#000000",
  "borderStyle": "solid",
  "borderWidth": 1.0,
  "borderRadius": 5.0,
  "layoutStyle": "simple",
  "showShadow": false,
  "showGradient": false,
  "qrSize": 25.0
}
```

### Template "Moderno"
```json
{
  "primaryColor": "#667eea",
  "secondaryColor": "#764ba2",
  "backgroundGradient": "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
  "textColor": "#ffffff",
  "layoutStyle": "modern",
  "showGradient": true,
  "borderRadius": 15.0,
  "qrSize": 28.0,
  "photoPosition": "left"
}
```

## 🔄 Próximos Pasos Recomendados

1. **Actualizar la Vista PrintCarnet.cshtml** para usar todas las nuevas propiedades de configuración
2. **Crear interfaz de administración** para configurar templates visualmente
3. **Implementar vista previa en tiempo real** de cambios de configuración
4. **Crear templates predefinidos** en base de datos durante inicialización
5. **Documentar todos los parámetros** de query string disponibles

## 📝 Notas Técnicas

- **Compatibilidad hacia atrás**: Los valores por defecto mantienen el comportamiento anterior
- **Reflection**: Se usa reflection para mapear `TemplateConfig` a `PrintCardConfig`, permitiendo flexibilidad
- **Conversión de tipos**: El método `ConvertValue` maneja conversiones comunes automáticamente
- **Logging**: Se registran advertencias si hay problemas al aplicar configuraciones de template
- **Prioridad**: Query string tiene la mayor prioridad, permitiendo override rápido

## 🚀 Ventajas del Sistema

1. **Altamente Configurable**: Más de 50 opciones de personalización
2. **Flexible**: Configuración via templates o query string
3. **Escalable**: Fácil agregar nuevas propiedades
4. **Mantenible**: Código organizado y bien documentado
5. **Compatible**: No rompe funcionalidad existente
6. **Performante**: Configuraciones se aplican eficientemente

