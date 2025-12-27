using System.Text.Json;

namespace CarnetQRPlatform.Domain.Entities;

public class CardTemplate : BaseEntity, ITenantEntity
{
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool PhotoEnabled { get; set; } = true;
    public List<string> VisibleFields { get; set; } = new();
    public string? TemplateHtml { get; set; }
    public Dictionary<string, object> TemplateConfig { get; set; } = new();

    public Institution Institution { get; set; } = null!;
}


