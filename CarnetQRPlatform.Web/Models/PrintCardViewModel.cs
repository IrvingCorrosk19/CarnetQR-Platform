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
    // Tamaño del carnet (en mm)
    public double Width { get; set; } = 85.6; // Tamaño estándar tarjeta (horizontal)
    public double Height { get; set; } = 54.0;
    public string Orientation { get; set; } = "horizontal"; // horizontal | vertical
    
    // Elementos visibles
    public bool ShowLogo { get; set; } = true;
    public bool ShowInstitutionName { get; set; } = true;
    public bool ShowUserName { get; set; } = true;
    public bool ShowCardNumber { get; set; } = true;
    public bool ShowQrCode { get; set; } = true;
    public bool ShowIdentificationNumber { get; set; } = false;
    public bool ShowEmail { get; set; } = false;
    public bool ShowPhone { get; set; } = false;
    
    // Colores (opcional para futuras mejoras)
    public string PrimaryColor { get; set; } = "#667eea";
    public string SecondaryColor { get; set; } = "#764ba2";
}

