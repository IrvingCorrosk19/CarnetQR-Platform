using Microsoft.AspNetCore.Identity;

namespace CarnetQRPlatform.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? InstitutionId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public Institution? Institution { get; set; }
}


