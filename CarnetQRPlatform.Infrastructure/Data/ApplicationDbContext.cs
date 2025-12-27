using CarnetQRPlatform.Domain.Entities;
using CarnetQRPlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace CarnetQRPlatform.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Institution> Institutions { get; set; }
    public DbSet<EntityProfile> EntityProfiles { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<CardTemplate> CardTemplates { get; set; }
    public DbSet<EventRecord> EventRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureInstitution(builder);
        ConfigureEntityProfile(builder);
        ConfigureCard(builder);
        ConfigureCardTemplate(builder);
        ConfigureEventRecord(builder);
        ConfigureAuditLog(builder);
        ConfigureAppUser(builder);

        // Query filters for multi-tenant entities (applied dynamically via service layer)
        // Note: Global query filters are static, so we handle filtering in SaveChanges
        // and through service layer queries
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var modifiedEntries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        // Convertir DateOfBirth a UTC para todas las entidades EntityProfile (PostgreSQL requiere UTC)
        var entityProfileEntries = ChangeTracker.Entries<EntityProfile>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in entityProfileEntries)
        {
            var entity = entry.Entity;
            if (entity.DateOfBirth.HasValue && entity.DateOfBirth.Value.Kind != DateTimeKind.Utc)
            {
                entity.DateOfBirth = DateTime.SpecifyKind(entity.DateOfBirth.Value, DateTimeKind.Utc);
            }
        }
        
        // Convertir ScheduledAt y CompletedAt a UTC para EventRecords (PostgreSQL requiere UTC)
        var eventRecordEntries = ChangeTracker.Entries<EventRecord>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in eventRecordEntries)
        {
            var entity = entry.Entity;
            if (entity.ScheduledAt.Kind != DateTimeKind.Utc)
            {
                entity.ScheduledAt = DateTime.SpecifyKind(entity.ScheduledAt, DateTimeKind.Utc);
            }
            if (entity.CompletedAt.HasValue && entity.CompletedAt.Value.Kind != DateTimeKind.Utc)
            {
                entity.CompletedAt = DateTime.SpecifyKind(entity.CompletedAt.Value, DateTimeKind.Utc);
            }
        }

        // VALIDACIÓN MULTI-TENANT ESTRICTA: Prevenir cambios de InstitutionId en updates
        var tenantEntityEntries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Modified);
        
        foreach (var entry in tenantEntityEntries)
        {
            var originalInstitutionId = entry.Property("InstitutionId").OriginalValue;
            var currentInstitutionId = entry.Property("InstitutionId").CurrentValue;
            
            // Si el InstitutionId cambió, es un intento de violación multi-tenant
            if (originalInstitutionId != null && currentInstitutionId != null && 
                !originalInstitutionId.Equals(currentInstitutionId))
            {
                throw new InvalidOperationException(
                    $"Multi-tenant violation: Cannot change InstitutionId from {originalInstitutionId} to {currentInstitutionId} for entity {entry.Entity.GetType().Name}");
            }
            
            // Restaurar el InstitutionId original si se intentó cambiar
            if (originalInstitutionId != null)
            {
                entry.Property("InstitutionId").CurrentValue = originalInstitutionId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    private void ConfigureInstitution(ModelBuilder builder)
    {
        builder.Entity<Institution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CardPrefix).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CardPrefix).IsUnique();
            
            // Configurar enum InstitutionType
            entity.Property(e => e.InstitutionType)
                .HasConversion<int>();
            
            // Configurar enum QrPublicDisplayMode
            entity.Property(e => e.QrPublicDisplayMode)
                .HasConversion<int>();
            
            // Configurar campos JSON
            var jsonOptions = new JsonSerializerOptions();
            
            entity.Property(e => e.VisibleFields)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "[]" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new List<string>(c)));
            
            entity.Property(e => e.PatientDataVisibilityConfig)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "{}" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, bool>() : JsonSerializer.Deserialize<Dictionary<string, bool>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, bool>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new Dictionary<string, bool>(c)));
        });
    }

    private void ConfigureEntityProfile(ModelBuilder builder)
    {
        builder.Entity<EntityProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstitutionId);
            entity.HasIndex(e => new { e.InstitutionId, e.IdentificationNumber });

            entity.HasOne(e => e.Institution)
                .WithMany(i => i.EntityProfiles)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            var jsonOptions = new JsonSerializerOptions();
            entity.Property(e => e.CustomFields)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "{}" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new Dictionary<string, object>(c)));
            
            // Configurar PatientDataVisibilityOverride
            entity.Property(e => e.PatientDataVisibilityOverride)
                .HasConversion(
                    v => v != null && v.Count > 0 ? JsonSerializer.Serialize(v, jsonOptions) : null,
                    v => !string.IsNullOrWhiteSpace(v) ? JsonSerializer.Deserialize<Dictionary<string, bool>>(v, jsonOptions) : null)
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, bool>?>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                    c => c != null ? new Dictionary<string, bool>(c) : null));
        });
    }

    private void ConfigureCard(ModelBuilder builder)
    {
        builder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstitutionId);
            entity.HasIndex(e => e.CardNumber).IsUnique();
            entity.HasIndex(e => e.QrToken).IsUnique();
            entity.HasIndex(e => e.EntityProfileId);

            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QrToken).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Institution)
                .WithMany(i => i.Cards)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EntityProfile)
                .WithMany(ep => ep.Cards)
                .HasForeignKey(e => e.EntityProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCardTemplate(ModelBuilder builder)
    {
        builder.Entity<CardTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstitutionId);

            entity.HasOne(e => e.Institution)
                .WithMany(i => i.CardTemplates)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            var jsonOptions = new JsonSerializerOptions();
            entity.Property(e => e.TemplateConfig)
                .HasConversion(
                    v => v == null || v.Count == 0 ? "{}" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new Dictionary<string, object>(c)));
        });
    }

    private void ConfigureEventRecord(ModelBuilder builder)
    {
        builder.Entity<EventRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstitutionId);
            entity.HasIndex(e => e.EntityProfileId);
            entity.HasIndex(e => e.ScheduledAt);

            entity.HasOne(e => e.Institution)
                .WithMany(i => i.EventRecords)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EntityProfile)
                .WithMany(ep => ep.EventRecords)
                .HasForeignKey(e => e.EntityProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstitutionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.Entity, e.EntityId });

            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Entity).IsRequired().HasMaxLength(100);

            var jsonOptions = new JsonSerializerOptions();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => v != null && v.Count > 0 ? JsonSerializer.Serialize(v, jsonOptions) : null,
                    v => !string.IsNullOrWhiteSpace(v) ? JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) : null)
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>?>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                    c => c != null ? new Dictionary<string, object>(c) : null));
        });
    }

    private void ConfigureAppUser(ModelBuilder builder)
    {
        builder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.InstitutionId);

            entity.HasOne(e => e.Institution)
                .WithMany(i => i.Users)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Allow null for SuperAdmin users
        });
    }
}


