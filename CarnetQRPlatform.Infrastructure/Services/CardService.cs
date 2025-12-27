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
        // MULTI-TENANT ESTRICTO: Obtener tenant del contexto actual
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        // Si no hay tenant y no es SuperAdmin, rechazar
        if (!tenantId.HasValue && !_tenantProvider.IsSuperAdmin())
        {
            throw new InvalidOperationException("Cannot create card without tenant context");
        }

        // Obtener EntityProfile con validación de tenant
        var entityProfileQuery = _context.EntityProfiles.AsQueryable();
        if (tenantId.HasValue)
        {
            entityProfileQuery = entityProfileQuery.Where(ep => ep.InstitutionId == tenantId.Value);
        }
        
        var entityProfile = await entityProfileQuery.FirstOrDefaultAsync(ep => ep.Id == entityProfileId);
        if (entityProfile == null)
        {
            throw new ArgumentException("EntityProfile not found or access denied");
        }

        // Usar InstitutionId del EntityProfile (ya validado)
        var institutionId = entityProfile.InstitutionId;
        var institution = await _context.Institutions.FindAsync(institutionId);
        if (institution == null)
        {
            throw new ArgumentException("Institution not found");
        }

        // Generate card number
        var lastCard = await _context.Cards
            .Where(c => c.InstitutionId == tenantId && c.CardNumber.StartsWith(institution.CardPrefix))
            .OrderByDescending(c => c.CardNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastCard != null)
        {
            var lastNumberStr = lastCard.CardNumber.Replace(institution.CardPrefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        var cardNumber = $"{institution.CardPrefix}{nextNumber:D6}";

        // Generate secure QR token
        var qrToken = GenerateSecureToken();

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

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

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

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, 32);
    }
}

