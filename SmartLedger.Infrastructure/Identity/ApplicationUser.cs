using Microsoft.AspNetCore.Identity;

namespace SmartLedger.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
