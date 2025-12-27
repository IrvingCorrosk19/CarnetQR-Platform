using CarnetQRPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarnetQRPlatform.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue && !context.Items.ContainsKey("TenantId"))
            {
                context.Items["TenantId"] = tenantId.Value;
            }
        }

        await _next(context);
    }
}


