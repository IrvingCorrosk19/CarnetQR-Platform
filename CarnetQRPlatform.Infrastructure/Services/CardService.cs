using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarnetQRPlatform.Infrastructure.Services;

public class CardService : ICardService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CardService(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<Card>> GetAllAsync()
    {
        var query = _context.Cards.Include(c => c.EntityProfile).Include(c => c.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).OrderByDescending(c => c.IssuedAt).ToListAsync();
    }

    public async Task<Card?> GetByIdAsync(Guid id)
    {
        var query = _context.Cards.Include(c => c.EntityProfile).Include(c => c.Institution).AsQueryable();
        return await query.ApplyTenantFilter(_tenantProvider).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Card?> GetByQrTokenAsync(string qrToken)
    {
        return await _context.Cards
            .Include(c => c.EntityProfile)
            .Include(c => c.Institution)
            .FirstOrDefaultAsync(c => c.QrToken == qrToken);
    }

    public async Task<Card> CreateAsync(Guid entityProfileId)
    {
        System.Console.WriteLine("=== [CardService] CreateAsync ===");
        System.Console.WriteLine($"[Service] EntityProfileId: {entityProfileId}");
        
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        System.Console.WriteLine($"[Service] TenantId: {tenantId}");
        System.Console.WriteLine($"[Service] IsSuperAdmin: {_tenantProvider.IsSuperAdmin()}");
        
        // Si no hay tenant y no es SuperAdmin, rechazar
        if (!tenantId.HasValue && !_tenantProvider.IsSuperAdmin())
        {
            System.Console.WriteLine("[Service] ERROR: Cannot create card without tenant context");
            throw new InvalidOperationException("Cannot create card without tenant context");
        }

        // Obtener EntityProfile con validación de tenant
        System.Console.WriteLine("[Service] Getting EntityProfile...");
        var entityProfileQuery = _context.EntityProfiles.AsQueryable();
        if (tenantId.HasValue)
        {
            entityProfileQuery = entityProfileQuery.Where(ep => ep.InstitutionId == tenantId.Value);
        }
        
        var entityProfile = await entityProfileQuery.FirstOrDefaultAsync(ep => ep.Id == entityProfileId);
        if (entityProfile == null)
        {
            System.Console.WriteLine("[Service] ERROR: EntityProfile not found or access denied");
            throw new ArgumentException("EntityProfile not found or access denied");
        }

        System.Console.WriteLine($"[Service] EntityProfile found: {entityProfile.FirstName} {entityProfile.LastName}, InstitutionId: {entityProfile.InstitutionId}");

        // Usar InstitutionId del EntityProfile (ya validado)
        var institutionId = entityProfile.InstitutionId;
        System.Console.WriteLine($"[Service] InstitutionId: {institutionId}");
        
        var institution = await _context.Institutions.FindAsync(institutionId);
        if (institution == null)
        {
            System.Console.WriteLine("[Service] ERROR: Institution not found");
            throw new ArgumentException("Institution not found");
        }

        System.Console.WriteLine($"[Service] Institution found: {institution.Name}, Prefix: {institution.CardPrefix}");

        // Generate card number
        System.Console.WriteLine("[Service] Generating card number...");
        var lastCard = await _context.Cards
            .Where(c => c.InstitutionId == institutionId && c.CardNumber.StartsWith(institution.CardPrefix))
            .OrderByDescending(c => c.CardNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastCard != null)
        {
            System.Console.WriteLine($"[Service] Last card found: {lastCard.CardNumber}");
            var lastNumberStr = lastCard.CardNumber.Replace(institution.CardPrefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }
        else
        {
            System.Console.WriteLine("[Service] No previous cards found, starting at 1");
        }

        var cardNumber = $"{institution.CardPrefix}{nextNumber:D6}";
        System.Console.WriteLine($"[Service] Generated card number: {cardNumber}");

        // Generate secure QR token
        System.Console.WriteLine("[Service] Generating QR token...");
        var qrToken = GenerateSecureToken();
        System.Console.WriteLine($"[Service] Generated QR token: {qrToken}");

        var card = new Card
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId, // Usar InstitutionId del EntityProfile validado
            EntityProfileId = entityProfileId,
            CardNumber = cardNumber,
            QrToken = qrToken,
            IssuedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        System.Console.WriteLine($"[Service] Card created - Id: {card.Id}, CardNumber: {card.CardNumber}, QrToken: {card.QrToken}");
        System.Console.WriteLine("[Service] Saving to database...");

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        System.Console.WriteLine("[Service] Card saved successfully!");
        System.Console.WriteLine("=== [CardService] CreateAsync END ===");

        return card;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var card = await GetByIdAsync(id);
        if (card == null) return false;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var card = await GetByIdAsync(id);
        if (card == null) return false;

        card.IsActive = !card.IsActive;
        card.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, 32);
    }
}

