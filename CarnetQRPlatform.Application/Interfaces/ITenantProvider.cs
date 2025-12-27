namespace CarnetQRPlatform.Application.Interfaces;

public interface ITenantProvider
{
    Guid? GetCurrentTenantId();
    bool IsSuperAdmin();
}


