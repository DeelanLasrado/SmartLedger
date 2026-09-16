using SmartLedger.API.Extensions;
using SmartLedger.Infrastructure.Common;

namespace SmartLedger.API.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                tenantContext.CurrentTenantId = context.User.GetTenantId();
            }
            catch (InvalidOperationException)
            {
                // Allow endpoints that don't require tenant claim (rare)
            }
        }

        await next(context);
    }
}
