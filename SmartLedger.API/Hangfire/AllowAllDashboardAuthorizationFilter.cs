using Hangfire.Dashboard;

namespace SmartLedger.API.Hangfire;

/// <summary>Open Hangfire dashboard for demo deployments. Restrict in production.</summary>
public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
