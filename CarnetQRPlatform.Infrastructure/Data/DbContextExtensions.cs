using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarnetQRPlatform.Infrastructure.Data;

public static class DbContextExtensions
{
    public static IQueryable<T> ApplyTenantFilter<T>(this IQueryable<T> query, ITenantProvider tenantProvider) 
        where T : class, ITenantEntity
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        
        if (tenantId.HasValue && !tenantProvider.IsSuperAdmin())
        {
            return query.Where(e => e.InstitutionId == tenantId.Value);
        }

        return query;
    }

    public static IQueryable<EntityProfile> GetTenantEntityProfiles(this ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        return context.EntityProfiles.ApplyTenantFilter(tenantProvider);
    }

    public static IQueryable<Card> GetTenantCards(this ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        return context.Cards.ApplyTenantFilter(tenantProvider);
    }

    public static IQueryable<CardTemplate> GetTenantCardTemplates(this ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        return context.CardTemplates.ApplyTenantFilter(tenantProvider);
    }

    public static IQueryable<EventRecord> GetTenantEventRecords(this ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        return context.EventRecords.ApplyTenantFilter(tenantProvider);
    }

    public static IQueryable<AuditLog> GetTenantAuditLogs(this ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        
        if (tenantId.HasValue && !tenantProvider.IsSuperAdmin())
        {
            return context.AuditLogs.Where(e => e.InstitutionId == tenantId.Value);
        }

        return context.AuditLogs;
    }
}

