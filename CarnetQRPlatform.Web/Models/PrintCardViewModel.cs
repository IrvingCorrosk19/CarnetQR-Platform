namespace CarnetQRPlatform.Web.Models;

public class PrintCardViewModel
{
    // Datos del Carnet
    public string CardNumber { get; set; } = string.Empty;
    public string QrToken { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    
    // Datos de la Entidad
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IdentificationNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhotoPath { get; set; }
    
    // Datos de la Institución
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionLogoPath { get; set; }
    
    // QR Code
    public string QrCodeBase64 { get; set; } = string.Empty;
    
    // Configuración de Impresión
    public PrintCardConfig Config { get; set; } = new();
}

public class PrintCardConfig
{
    // ==================== TAMAÑO Y ORIENTACIÓN ====================
    public double Width { get; set; } = 85.6; // Tamaño estándar tarjeta (horizontal) en mm
    public double Height { get; set; } = 54.0; // mm
    public string Orientation { get; set; } = "horizontal"; // horizontal | vertical
    
    // ==================== DOS CARAS ====================
    public bool DoubleSided { get; set; } = true; // Impresión de dos caras (frente/trasera) - Por defecto activado
    public bool QrOnBack { get; set; } = true; // QR en la parte trasera (requiere DoubleSided = true) - Por defecto activado
    public bool BackRotate180 { get; set; } = false; // Rotar 180° la trasera para impresoras de dos caras
    
    // ==================== COLORES Y ESTILOS ====================
    public string PrimaryColor { get; set; } = "#1e3a8a"; // Azul corporativo oscuro profesional
    public string SecondaryColor { get; set; } = "#3b82f6"; // Azul medio corporativo
    public string BackgroundColor { get; set; } = "#ffffff"; // Blanco limpio
    public string BackgroundGradient { get; set; } = ""; // Sin gradiente por defecto (diseño limpio)
    public string TextColor { get; set; } = "#1f2937"; // Gris oscuro profesional
    public string BorderColor { get; set; } = "#e5e7eb"; // Gris claro suave
    public string BorderStyle { get; set; } = "solid"; // solid, dashed, dotted, double
    public double BorderWidth { get; set; } = 1.0; // mm - borde más delgado y profesional
    public double BorderRadius { get; set; } = 5.0; // mm - esquinas más sutiles
    
    // ==================== FUENTES ====================
    public string FontFamily { get; set; } = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif";
    public double FontSizeName { get; set; } = 13.0; // pt
    public double FontSizeCardNumber { get; set; } = 10.0; // pt
    public double FontSizeDetails { get; set; } = 8.0; // pt
    public string FontWeightName { get; set; } = "700"; // bold: 700, normal: 400
    public string FontWeightDetails { get; set; } = "400"; // normal
    
    // ==================== TAMAÑOS DE ELEMENTOS ====================
    public double PhotoWidth { get; set; } = 25.0; // mm
    public double PhotoHeight { get; set; } = 30.0; // mm
    public double QrSize { get; set; } = 28.0; // mm (frente)
    public double QrBackSize { get; set; } = 35.0; // mm (trasera)
    public double LogoWidth { get; set; } = 25.0; // mm
    public double LogoHeight { get; set; } = 15.0; // mm
    
    // ==================== POSICIONAMIENTO ====================
    public string LogoPosition { get; set; } = "top-left"; // top-left, top-right, top-center
    public string PhotoPosition { get; set; } = "left"; // left, right, top, center
    public string QrPosition { get; set; } = "right"; // top-left, top-right, bottom-left, bottom-right, center, left, right
    public string QrBackPosition { get; set; } = "center"; // center, top, bottom, left, right
    public string TextAlignment { get; set; } = "left"; // left, center, right, justify
    
    // ==================== LAYOUTS PREDEFINIDOS ====================
    public string LayoutStyle { get; set; } = "standard"; // standard, compact, expanded, professional, simple
    
    // ==================== ELEMENTOS VISIBLES EN EL FRENTE ====================
    public bool ShowLogo { get; set; } = true;
    public bool ShowInstitutionName { get; set; } = true;
    public bool ShowUserName { get; set; } = true;
    public bool ShowCardNumber { get; set; } = true;
    public bool ShowQrCode { get; set; } = false; // Por defecto no se muestra en el frente (va en la trasera)
    public bool ShowPhoto { get; set; } = true; // Por defecto mostrar foto si existe
    public bool ShowIdentificationNumber { get; set; } = false;
    public bool ShowEmail { get; set; } = false;
    public bool ShowPhone { get; set; } = false;
    public bool ShowDateOfBirth { get; set; } = false;
    public bool ShowIssuedDate { get; set; } = true;
    
    // ==================== ESPACIADO Y MÁRGENES ====================
    public double Padding { get; set; } = 6.0; // mm - padding interno del carnet
    public double SpacingBetweenElements { get; set; } = 4.0; // mm - espacio entre elementos
    public double MarginTop { get; set; } = 0.0; // mm
    public double MarginBottom { get; set; } = 0.0; // mm
    public double MarginLeft { get; set; } = 0.0; // mm
    public double MarginRight { get; set; } = 0.0; // mm
    
    // ==================== TRASERA ====================
    public string BackContent { get; set; } = "qr"; // qr, info, custom
    public string BackTextAlignment { get; set; } = "center"; // left, center, right
    public string BackBackgroundColor { get; set; } = "#ffffff"; // Blanco limpio y profesional
    public string BackInstructions { get; set; } = "Escanea el código QR para verificar la información del carnet";
    public bool BackShowInstitutionName { get; set; } = true;
    public bool BackShowCardNumber { get; set; } = true;
    public bool BackShowContactInfo { get; set; } = false; // Mostrar teléfono/dirección de institución
    
    // ==================== EFECTOS VISUALES ====================
    public bool ShowShadow { get; set; } = true;
    public double ShadowOpacity { get; set; } = 0.15;
    public bool ShowGradient { get; set; } = true; // Mostrar gradiente de fondo
    public string Watermark { get; set; } = ""; // Texto o path de imagen para watermark
    public double WatermarkOpacity { get; set; } = 0.05;
    public string WatermarkPosition { get; set; } = "center"; // center, top-left, top-right, etc.
    
    // ==================== CAMPOS PERSONALIZADOS ====================
    public Dictionary<string, string> CustomFields { get; set; } = new(); // Campos personalizados adicionales
    public List<string> CustomFieldsOrder { get; set; } = new(); // Orden de campos personalizados
    
    // ==================== FORMATOS ====================
    public string DateFormat { get; set; } = "dd/MM/yyyy"; // Formato de fecha
    public string TimeFormat { get; set; } = ""; // Formato de hora (si aplica)
    public string FooterText { get; set; } = ""; // Texto personalizado en footer
    public string BackContactInfo { get; set; } = ""; // Info de contacto para trasera (teléfono, dirección, etc.)
    
    // ==================== IMPRESIÓN ====================
    public string PrintResolution { get; set; } = "300dpi"; // 150dpi, 300dpi, 600dpi
    public string ColorMode { get; set; } = "RGB"; // RGB, CMYK (para impresión profesional)
    public bool OptimizeForPrint { get; set; } = true; // Optimizaciones para impresión
}

