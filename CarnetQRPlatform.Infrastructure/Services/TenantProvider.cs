using CarnetQRPlatform.Application.Interfaces;
using CarnetQRPlatform.Domain.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarnetQRPlatform.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        if (IsSuperAdmin())
            return null;

        // Try to get InstitutionId from claim
        // The claim will be set during login (see AccountController)
        var tenantIdClaim = httpContext.User?.FindFirst("InstitutionId");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    public bool IsSuperAdmin()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return false;

        return httpContext.User.IsInRole(Roles.SuperAdmin);
    }
}


