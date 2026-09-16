using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.CashFlow;

/// <summary>
/// Offline cash-flow forecaster using a moving-average / SSA-inspired smoother over daily invoice totals.
/// </summary>
public class CashFlowForecaster(IInvoiceRepository invoiceRepository) : ICashFlowForecaster
{
    public async Task<CashFlowForecastResult> ForecastAsync(
        Guid tenantId, int horizonDays, CancellationToken ct = default)
    {
        var invoices = await invoiceRepository.GetByTenantAsync(tenantId, 0, 500, ct);
        var daily = invoices
            .Where(i => i.InvoiceDate.HasValue)
            .GroupBy(i => i.InvoiceDate!.Value.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToList();

        var window = Math.Min(14, Math.Max(3, daily.Count));
        var avgDaily = daily.Count == 0
            ? 0m
            : daily.TakeLast(window).Average(x => x.Total);

        // Simple trend: compare recent half vs prior half
        var trend = 0m;
        if (daily.Count >= 6)
        {
            var mid = daily.Count / 2;
            var older = daily.Take(mid).DefaultIfEmpty().Average(x => x?.Total ?? 0);
            var newer = daily.Skip(mid).DefaultIfEmpty().Average(x => x?.Total ?? 0);
            trend = (newer - older) / Math.Max(mid, 1);
        }

        var startBalance = invoices.Sum(i => i.TotalAmount) * -1; // treat invoices as cash outflows
        var volatility = daily.Count < 2
            ? avgDaily * 0.15m
            : (decimal)Math.Sqrt((double)daily.Average(d =>
            {
                var diff = d.Total - avgDaily;
                return diff * diff;
            }));

        var points = new List<CashFlowPoint>();
        var balance = startBalance;
        var today = DateTime.UtcNow.Date;

        for (var day = 1; day <= horizonDays; day++)
        {
            var predictedOutflow = Math.Max(0, avgDaily + trend * day);
            balance -= predictedOutflow;
            var band = volatility * (decimal)Math.Sqrt(day);
            points.Add(new CashFlowPoint(
                today.AddDays(day),
                Math.Round(balance, 2),
                Math.Round(balance - band, 2),
                Math.Round(balance + band, 2)));
        }

        var lower = points.Count == 0 ? 0 : points.Min(p => p.LowerBound);
        var upper = points.Count == 0 ? 0 : points.Max(p => p.UpperBound);

        return new CashFlowForecastResult(horizonDays, points, lower, upper);
    }
}
