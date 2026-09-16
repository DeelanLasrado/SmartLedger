using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.AnomalyDetection;

/// <summary>
/// Statistical anomaly detector using category history mean and a 3× multiplier rule.
/// Optionally extendable with ML.NET time-series (SSA) when more training data is available.
/// </summary>
public class AnomalyDetector(
    IInvoiceRepository invoiceRepository,
    ILogger<AnomalyDetector> logger) : IAnomalyDetector
{
    private const decimal MultiplierThreshold = 3m;
    private const int LookbackMonths = 6;

    public async Task<AnomalyResult> CheckAsync(Invoice invoice, CancellationToken ct = default)
    {
        var history = await invoiceRepository.GetRecentByCategoryAsync(
            invoice.TenantId, invoice.Category, LookbackMonths, ct);

        var prior = history.Where(i => i.Id != invoice.Id).ToList();
        if (prior.Count < 2)
        {
            logger.LogDebug("Insufficient history for category {Category}; skipping anomaly.", invoice.Category);
            return new AnomalyResult(false, "Insufficient category history", invoice.TotalAmount);
        }

        var amounts = prior.Select(i => i.TotalAmount).ToList();
        var mean = amounts.Average();
        var variance = amounts.Sum(a => (a - mean) * (a - mean)) / amounts.Count;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        var expected = Math.Round(mean, 2);
        var zScore = stdDev > 0 ? (invoice.TotalAmount - mean) / stdDev : 0;

        var isAnomaly = invoice.TotalAmount > mean * MultiplierThreshold
                        || (stdDev > 0 && zScore > 3m);

        if (!isAnomaly)
            return new AnomalyResult(false, "Within normal range", expected);

        var reason =
            $"Amount ₹{invoice.TotalAmount:N2} is unusual vs category '{invoice.Category}' average ₹{expected:N2} " +
            $"(threshold {MultiplierThreshold}× / z≈{zScore:F1}).";

        logger.LogWarning("Anomaly detected for invoice {InvoiceId}: {Reason}", invoice.Id, reason);
        return new AnomalyResult(true, reason, expected);
    }
}
