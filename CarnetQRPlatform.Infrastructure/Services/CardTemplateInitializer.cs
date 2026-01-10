using System.Text.Json;
using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Services;

/// <summary>
/// Servicio para inicializar templates predefinidos de carnet
/// </summary>
public class CardTemplateInitializer
{
    private readonly ApplicationDbContext _context;

    public CardTemplateInitializer(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Inicializa templates predefinidos para una institución si no existen
    /// </summary>
    public async Task InitializeDefaultTemplatesAsync(Guid institutionId)
    {
        var existingTemplates = await _context.CardTemplates
            .Where(t => t.InstitutionId == institutionId)
            .AnyAsync();

        if (existingTemplates)
        {
            return; // Ya existen templates, no inicializar
        }

        var templates = new List<CardTemplate>
        {
            CreateProfessionalTemplate(institutionId),
            CreateSimpleTemplate(institutionId),
            CreateModernTemplate(institutionId),
            CreateMinimalistTemplate(institutionId),
            CreateCompactTemplate(institutionId)
        };

        _context.CardTemplates.AddRange(templates);
        await _context.SaveChangesAsync();
    }

    private CardTemplate CreateProfessionalTemplate(Guid institutionId)
    {
        var config = new Dictionary<string, object>
        {
            { "primaryColor", "#2c3e50" },
            { "secondaryColor", "#34495e" },
            { "backgroundColor", "#ffffff" },
            { "textColor", "#2c3e50" },
            { "borderColor", "#2c3e50" },
            { "borderWidth", 2.0 },
            { "borderRadius", 10.0 },
            { "borderStyle", "solid" },
            { "photoPosition", "left" },
            { "qrPosition", "right" },
            { "qrSize", 30.0 },
            { "photoWidth", 28.0 },
            { "photoHeight", 35.0 },
            { "layoutStyle", "professional" },
            { "fontSizeName", 14.0 },
            { "fontWeightName", "700" },
            { "showShadow", true },
            { "showGradient", false },
            { "qrOnBack", true },
            { "doubleSided", true },
            { "padding", 6.0 },
            { "spacingBetweenElements", 4.0 }
        };

        return new CardTemplate
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = "Profesional",
            IsDefault = true,
            PhotoEnabled = true,
            VisibleFields = new List<string> { "IdentificationNumber", "Email" },
            TemplateConfig = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    private CardTemplate CreateSimpleTemplate(Guid institutionId)
    {
        var config = new Dictionary<string, object>
        {
            { "primaryColor", "#333333" },
            { "backgroundColor", "#ffffff" },
            { "textColor", "#000000" },
            { "borderColor", "#cccccc" },
            { "borderWidth", 1.0 },
            { "borderRadius", 5.0 },
            { "borderStyle", "solid" },
            { "qrPosition", "right" },
            { "qrSize", 25.0 },
            { "layoutStyle", "simple" },
            { "fontSizeName", 12.0 },
            { "fontWeightName", "600" },
            { "showShadow", false },
            { "showGradient", false },
            { "qrOnBack", false },
            { "doubleSided", false },
            { "padding", 5.0 },
            { "spacingBetweenElements", 3.0 }
        };

        return new CardTemplate
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = "Simple",
            IsDefault = false,
            PhotoEnabled = false,
            VisibleFields = new List<string> { "IdentificationNumber" },
            TemplateConfig = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    private CardTemplate CreateModernTemplate(Guid institutionId)
    {
        var config = new Dictionary<string, object>
        {
            { "primaryColor", "#667eea" },
            { "secondaryColor", "#764ba2" },
            { "backgroundGradient", "linear-gradient(135deg, #667eea 0%, #764ba2 100%)" },
            { "backgroundColor", "#667eea" },
            { "textColor", "#ffffff" },
            { "borderColor", "#ffffff" },
            { "borderWidth", 2.0 },
            { "borderRadius", 15.0 },
            { "borderStyle", "solid" },
            { "photoPosition", "left" },
            { "qrPosition", "right" },
            { "qrSize", 28.0 },
            { "photoWidth", 26.0 },
            { "photoHeight", 32.0 },
            { "layoutStyle", "modern" },
            { "fontSizeName", 13.0 },
            { "fontWeightName", "700" },
            { "showShadow", true },
            { "showGradient", true },
            { "qrOnBack", false },
            { "doubleSided", false },
            { "padding", 6.0 },
            { "spacingBetweenElements", 4.0 }
        };

        return new CardTemplate
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = "Moderno",
            IsDefault = false,
            PhotoEnabled = true,
            VisibleFields = new List<string> { "Email", "Phone" },
            TemplateConfig = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    private CardTemplate CreateMinimalistTemplate(Guid institutionId)
    {
        var config = new Dictionary<string, object>
        {
            { "primaryColor", "#6c757d" },
            { "backgroundColor", "#ffffff" },
            { "textColor", "#333333" },
            { "borderColor", "#dee2e6" },
            { "borderWidth", 1.0 },
            { "borderRadius", 8.0 },
            { "borderStyle", "solid" },
            { "qrPosition", "right" },
            { "qrSize", 24.0 },
            { "layoutStyle", "simple" },
            { "fontSizeName", 11.0 },
            { "fontWeightName", "500" },
            { "fontSizeDetails", 8.0 },
            { "showShadow", false },
            { "showGradient", false },
            { "showLogo", false },
            { "showPhoto", false },
            { "qrOnBack", false },
            { "doubleSided", false },
            { "padding", 4.0 },
            { "spacingBetweenElements", 2.0 }
        };

        return new CardTemplate
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = "Minimalista",
            IsDefault = false,
            PhotoEnabled = false,
            VisibleFields = new List<string> { "IdentificationNumber" },
            TemplateConfig = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    private CardTemplate CreateCompactTemplate(Guid institutionId)
    {
        var config = new Dictionary<string, object>
        {
            { "primaryColor", "#495057" },
            { "secondaryColor", "#6c757d" },
            { "backgroundColor", "#ffffff" },
            { "textColor", "#212529" },
            { "borderColor", "#adb5bd" },
            { "borderWidth", 1.5 },
            { "borderRadius", 6.0 },
            { "borderStyle", "solid" },
            { "photoPosition", "left" },
            { "qrPosition", "right" },
            { "qrSize", 22.0 },
            { "photoWidth", 20.0 },
            { "photoHeight", 25.0 },
            { "layoutStyle", "compact" },
            { "fontSizeName", 11.0 },
            { "fontSizeCardNumber", 9.0 },
            { "fontSizeDetails", 7.0 },
            { "fontWeightName", "600" },
            { "showShadow", false },
            { "showGradient", false },
            { "qrOnBack", false },
            { "doubleSided", false },
            { "padding", 4.0 },
            { "spacingBetweenElements", 2.5 }
        };

        return new CardTemplate
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = "Compacto",
            IsDefault = false,
            PhotoEnabled = true,
            VisibleFields = new List<string> { "IdentificationNumber", "Email", "Phone" },
            TemplateConfig = config,
            CreatedAt = DateTime.UtcNow
        };
    }
}

