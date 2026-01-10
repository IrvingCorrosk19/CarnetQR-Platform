using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Application.Services;
using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Infrastructure.Data;
using CarnetQRPlatform.Application.Common;
using CarnetQRPlatform.Application.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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

    public async Task<PagedResult<Card>> GetAllPagedAsync(PaginationParameters parameters)
    {
        var query = _context.Cards
            .Include(c => c.EntityProfile)
            .Include(c => c.Institution)
            .AsQueryable();
        
        return await query
            .ApplyTenantFilter(_tenantProvider)
            .OrderByDescending(c => c.IssuedAt)
            .ToPagedResultAsync(parameters);
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

        // Generate unique card number
        System.Console.WriteLine("[Service] Generating card number...");
        var cardNumber = await GenerateUniqueCardNumberAsync(institutionId, institution.CardPrefix);
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
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Unique constraint violation - probablemente race condition en generación de número de carnet
            // o duplicado de QR token (muy poco probable)
            if (pgEx.ConstraintName?.Contains("CardNumber") == true)
            {
                System.Console.WriteLine($"[Service] ERROR: CardNumber duplicate detected (race condition). Retrying...");
                // Remover el card del contexto y crear uno nuevo con número único
                _context.Entry(card).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                var retryCardNumber = await GenerateUniqueCardNumberAsync(institutionId, institution.CardPrefix);
                card.CardNumber = retryCardNumber;
                card.Id = Guid.NewGuid(); // Nuevo ID para evitar conflictos
                _context.Cards.Add(card);
                await _context.SaveChangesAsync();
            }
            else if (pgEx.ConstraintName?.Contains("QrToken") == true)
            {
                // QR Token duplicado (extremadamente poco probable, pero manejado)
                System.Console.WriteLine($"[Service] ERROR: QrToken duplicate detected. Generating new token...");
                _context.Entry(card).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                card.QrToken = GenerateSecureToken();
                card.Id = Guid.NewGuid(); // Nuevo ID para evitar conflictos
                _context.Cards.Add(card);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw;
            }
        }

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

    private async Task<string> GenerateUniqueCardNumberAsync(Guid institutionId, string prefix)
    {
        const int maxRetries = 10;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var lastCard = await _context.Cards
                .Where(c => c.InstitutionId == institutionId && c.CardNumber.StartsWith(prefix))
                .OrderByDescending(c => c.CardNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCard != null)
            {
                var lastNumberStr = lastCard.CardNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            var cardNumber = $"{prefix}{nextNumber:D6}";
            
            // Verificar que no existe (double-check para evitar race conditions)
            var exists = await _context.Cards.AnyAsync(c => c.CardNumber == cardNumber);
            if (!exists)
            {
                return cardNumber;
            }
            
            // Si existe, esperar un poco y reintentar (en caso de race condition)
            await Task.Delay(50 * (attempt + 1));
        }
        
        throw new InvalidOperationException($"No se pudo generar un número de carnet único después de {maxRetries} intentos.");
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        
        // Convert to Base64 URL-safe string and ensure exactly 32 characters
        var base64 = Convert.ToBase64String(bytes);
        var urlSafe = base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        
        // Ensure we have at least 32 characters (Base64 of 32 bytes = 44 chars, so this is safe)
        // But take first 32 to ensure consistent length
        return urlSafe.Length >= 32 ? urlSafe.Substring(0, 32) : urlSafe.PadRight(32, '0');
    }
}

