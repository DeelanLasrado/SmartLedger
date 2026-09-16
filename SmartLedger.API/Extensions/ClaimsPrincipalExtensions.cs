using System.Security.Claims;

namespace SmartLedger.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenant_id")
                    ?? user.FindFirstValue("TenantId");

        if (Guid.TryParse(value, out var tenantId))
            return tenantId;

        throw new InvalidOperationException("TenantId claim is missing from the token.");
    }

    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");
}
