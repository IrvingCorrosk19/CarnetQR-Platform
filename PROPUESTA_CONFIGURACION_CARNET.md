# Propuesta: Hacer el Carnet Más Configurable

## 🎯 Objetivo
Aumentar significativamente la personalización del diseño del carnet sin modificar código.

## 📋 Opciones de Configuración Propuestas

### 1. **Sistema de Templates** (Prioridad Alta)
- Usar la entidad `CardTemplate` existente (actualmente no se usa)
- Permitir múltiples templates por institución
- Templates predefinidos (Profesional, Simple, Moderno, Minimalista, etc.)
- Template por defecto configurable
- Vista previa de templates

### 2. **Configuración Visual Avanzada** (Prioridad Alta)

#### A. Colores y Estilos
- Color primario personalizable
- Color secundario personalizable
- Color de fondo (sólido o gradiente)
- Color de texto
- Color de bordes
- Estilo de borde (sólido, punteado, doble)
- Grosor de borde
- Esquinas redondeadas (radius)

#### B. Posicionamiento de Elementos
- Posición del logo (top-left, top-right, top-center, bottom-left, etc.)
- Posición de la foto (left, right, top, center, bottom)
- Posición del QR (front: top-left, top-right, bottom-left, bottom-right, center)
- Posición del QR trasero (center, top, bottom, left, right)
- Orden de elementos (flex layout)
- Alineación de texto (left, center, right, justify)

#### C. Tamaños y Fuentes
- Tamaño de foto personalizable (ancho x alto en mm)
- Tamaño de QR personalizable (en mm)
- Tamaño de logo personalizable
- Tamaño de fuente para nombre (pt)
- Tamaño de fuente para número de carnet (pt)
- Tamaño de fuente para detalles (pt)
- Fuente personalizada (Arial, Times, Helvetica, etc.)
- Peso de fuente (normal, bold, light)
- Altura de línea (line-height)

#### D. Layouts Predefinidos
- Layout horizontal estándar (actual)
- Layout vertical
- Layout compacto (elementos más pequeños)
- Layout expandido (más espacio)
- Layout profesional (foto grande, info minimalista)
- Layout simple (sin decoraciones)

### 3. **Configuración de Campos** (Prioridad Media)

#### A. Campos Personalizados
- Usar `CustomFields` de EntityProfile
- Agregar campos personalizados al carnet
- Configurar visibilidad de campos personalizados
- Ordenar campos personalizados

#### B. Formato de Fechas
- Formato de fecha personalizable (dd/MM/yyyy, MM/dd/yyyy, etc.)
- Formato de hora (si aplica)
- Zona horaria

#### C. Información Adicional
- Texto personalizado (watermark, lema, slogan)
- Información de contacto (teléfono, dirección) en trasera
- Términos y condiciones (texto pequeño en trasera)
- URL del sitio web
- Redes sociales (opcional)

### 4. **Configuración de Dos Caras** (Prioridad Media)
- Contenido del frente configurable
- Contenido de la trasera configurable
- Rotación de segunda página (180° para impresoras)
- Vista previa de ambas caras
- Configuración separada por cara

### 5. **Imágenes y Fondos** (Prioridad Baja)
- Imagen de fondo personalizable (opcional)
- Watermark de institución
- Patrón de fondo (líneas, puntos, cuadrícula)
- Opacidad de fondo
- Imagen de marca de agua

### 6. **Configuración de Impresión** (Prioridad Media)
- Márgenes personalizables
- Tamaño de papel personalizado
- Orientación (horizontal/vertical)
- Resolución de impresión
- Calidad de imagen QR
- Modo de color (RGB, CMYK para impresión profesional)

### 7. **Configuración por Institución** (Prioridad Alta)
- Guardar configuración en `Institution` o `CardTemplate`
- Aplicar configuración por defecto a todos los carnets
- Permitir override por carnet individual (vía query string)
- Configuración heredable de template

## 🔧 Implementación Propuesta

### Fase 1: Extender PrintCardConfig (Implementación Inmediata)
- Agregar todas las propiedades de configuración visual
- Mantener compatibilidad hacia atrás
- Valores por defecto razonables

### Fase 2: Integrar CardTemplate (Implementación Rápida)
- Usar template por defecto al imprimir
- Permitir seleccionar template vía query string
- Cargar configuración desde TemplateConfig

### Fase 3: Interfaz de Configuración (Implementación Media)
- Vista para configurar templates
- Editor visual (drag & drop?)
- Vista previa en tiempo real
- Guardar/cargar templates

### Fase 4: Campos Personalizados (Implementación Media)
- Editor de campos personalizados
- Mapeo de CustomFields a vistas
- Configuración de visibilidad

## 📝 Propuesta de Código

### Modelo Extendido: PrintCardConfig

```csharp
public class PrintCardConfig
{
    // Tamaño y Orientación
    public double Width { get; set; } = 85.6;
    public double Height { get; set; } = 54.0;
    public string Orientation { get; set; } = "horizontal";
    
    // Dos Caras
    public bool DoubleSided { get; set; } = false;
    public bool QrOnBack { get; set; } = false;
    
    // Colores
    public string PrimaryColor { get; set; } = "#667eea";
    public string SecondaryColor { get; set; } = "#764ba2";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#2c3e50";
    public string BorderColor { get; set; } = "#667eea";
    public string BackgroundGradient { get; set; } = ""; // "linear-gradient(135deg, #667eea 0%, #764ba2 100%)"
    
    // Bordes y Estilos
    public double BorderWidth { get; set; } = 2.0; // mm
    public string BorderStyle { get; set; } = "solid"; // solid, dashed, dotted, double
    public double BorderRadius { get; set; } = 10.0; // mm
    
    // Fuentes
    public string FontFamily { get; set; } = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif";
    public double FontSizeName { get; set; } = 13.0; // pt
    public double FontSizeCardNumber { get; set; } = 10.0; // pt
    public double FontSizeDetails { get; set; } = 8.0; // pt
    public string FontWeightName { get; set; } = "700"; // bold
    public string FontWeightDetails { get; set; } = "400"; // normal
    
    // Tamaños de Elementos
    public double PhotoWidth { get; set; } = 25.0; // mm
    public double PhotoHeight { get; set; } = 30.0; // mm
    public double QrSize { get; set; } = 28.0; // mm
    public double QrBackSize { get; set; } = 35.0; // mm
    public double LogoWidth { get; set; } = 25.0; // mm
    public double LogoHeight { get; set; } = 15.0; // mm
    
    // Posicionamiento
    public string LogoPosition { get; set; } = "top-left"; // top-left, top-right, top-center, etc.
    public string PhotoPosition { get; set; } = "left"; // left, right, top, center
    public string QrPosition { get; set; } = "right"; // top-left, top-right, bottom-left, bottom-right, center, left, right
    public string TextAlignment { get; set; } = "left"; // left, center, right, justify
    public string LayoutStyle { get; set; } = "standard"; // standard, compact, expanded, professional, simple
    
    // Elementos Visibles
    public bool ShowLogo { get; set; } = true;
    public bool ShowInstitutionName { get; set; } = true;
    public bool ShowUserName { get; set; } = true;
    public bool ShowCardNumber { get; set; } = true;
    public bool ShowQrCode { get; set; } = true;
    public bool ShowPhoto { get; set; } = false;
    public bool ShowIdentificationNumber { get; set; } = false;
    public bool ShowEmail { get; set; } = false;
    public bool ShowPhone { get; set; } = false;
    public bool ShowDateOfBirth { get; set; } = false;
    public bool ShowIssuedDate { get; set; } = true;
    
    // Espaciado
    public double Padding { get; set; } = 6.0; // mm
    public double SpacingBetweenElements { get; set; } = 4.0; // mm
    public double MarginTop { get; set; } = 0.0; // mm
    public double MarginBottom { get; set; } = 0.0; // mm
    public double MarginLeft { get; set; } = 0.0; // mm
    public double MarginRight { get; set; } = 0.0; // mm
    
    // Trasera
    public string BackContent { get; set; } = "qr"; // qr, info, custom
    public string BackTextAlignment { get; set; } = "center";
    public bool BackRotate180 { get; set; } = false; // Para impresoras de dos caras
    public string BackBackgroundColor { get; set; } = "#f8f9fa";
    public string BackInstructions { get; set; } = "Escanea el código QR para verificar la información del carnet";
    
    // Efectos Visuales
    public bool ShowShadow { get; set; } = true;
    public double ShadowOpacity { get; set; } = 0.15;
    public bool ShowGradient { get; set; } = true;
    public string Watermark { get; set; } = ""; // Texto o path de imagen
    public double WatermarkOpacity { get; set; } = 0.05;
    
    // Información Adicional
    public Dictionary<string, string> CustomFields { get; set; } = new(); // Campos personalizados
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string FooterText { get; set; } = ""; // Texto personalizado en footer
    public string BackContactInfo { get; set; } = ""; // Info de contacto para trasera
}
```

## 🎨 Templates Predefinidos Propuestos

1. **Profesional**
   - Colores corporativos
   - Foto grande
   - Información minimalista
   - QR en trasera

2. **Simple**
   - Sin decoraciones
   - Sin gradientes
   - Información esencial
   - QR pequeño en frente

3. **Moderno**
   - Colores vibrantes
   - Gradientes suaves
   - Layout asimétrico
   - QR en frente

4. **Minimalista**
   - Solo texto y QR
   - Sin foto
   - Sin logo
   - Colores neutros

5. **Compacto**
   - Elementos pequeños
   - Todo en frente
   - Máxima información
   - QR pequeño

## 🔄 Integración con CardTemplate

Usar `CardTemplate.TemplateConfig` para guardar toda la configuración en JSON:

```json
{
  "primaryColor": "#667eea",
  "secondaryColor": "#764ba2",
  "qrOnBack": true,
  "photoPosition": "left",
  "layoutStyle": "professional",
  ...
}
```

## 📍 Próximos Pasos

1. ✅ Extender PrintCardConfig con todas las opciones
2. ✅ Modificar vista para usar las nuevas configuraciones
3. ✅ Integrar CardTemplate en el proceso de impresión
4. ⏳ Crear interfaz para configurar templates
5. ⏳ Implementar templates predefinidos
6. ⏳ Agregar vista previa en tiempo real

