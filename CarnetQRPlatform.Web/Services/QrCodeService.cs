using QRCoder;

namespace CarnetQRPlatform.Web.Services;

public class QrCodeService
{
    /// <summary>
    /// Genera un código QR en formato Base64 a partir de una URL
    /// </summary>
    /// <param name="url">URL a codificar en el QR</param>
    /// <param name="size">Tamaño aproximado del QR (se ajusta automáticamente según los módulos)</param>
    /// <returns>String Base64 de la imagen del QR (formato: "data:image/png;base64,...")</returns>
    public string GenerateQrCodeBase64(string url, int size = 300)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        
        // Calcular el tamaño de los módulos basado en el tamaño deseado
        // QRCoder genera QR con ~25 módulos por defecto, así que ajustamos
        int pixelsPerModule = Math.Max(8, size / 25);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

        var base64String = Convert.ToBase64String(qrCodeBytes);
        return $"data:image/png;base64,{base64String}";
    }

    /// <summary>
    /// Genera un código QR en formato Base64 con opciones personalizadas
    /// </summary>
    /// <param name="url">URL a codificar en el QR</param>
    /// <param name="size">Tamaño aproximado del QR</param>
    /// <param name="darkColor">Color oscuro del QR (hex, ej: "#000000")</param>
    /// <param name="lightColor">Color claro del QR (hex, ej: "#FFFFFF")</param>
    /// <returns>String Base64 de la imagen del QR</returns>
    public string GenerateQrCodeBase64(string url, int size, string darkColor, string lightColor)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        
        // Convertir colores hex a RGB
        var darkRgb = HexToRgb(darkColor);
        var lightRgb = HexToRgb(lightColor);
        
        // Calcular el tamaño de los módulos
        int pixelsPerModule = Math.Max(8, size / 25);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule, darkRgb, lightRgb);

        var base64String = Convert.ToBase64String(qrCodeBytes);
        return $"data:image/png;base64,{base64String}";
    }

    /// <summary>
    /// Convierte un color hexadecimal a RGB
    /// </summary>
    private byte[] HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
        {
            throw new ArgumentException("Invalid hex color format", nameof(hex));
        }

        return new byte[]
        {
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16)
        };
    }
}

