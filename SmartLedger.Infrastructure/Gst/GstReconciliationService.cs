using SmartLedger.Domain.Enums;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Gst;

public class GstReconciliationService(
    IGstEntryRepository gstEntryRepository) : IGstReconciliationService
{
    public async Task<GstReconciliationResult> ReconcileAsync(
        Guid tenantId, string period, CancellationToken ct = default)
    {
        var entries = await gstEntryRepository.GetByPeriodAsync(tenantId, period, ct);
        var gstr2A = entries.Where(e => e.ReturnType == GstReturnType.Gstr2A).ToList();
        var gstr3B = entries.Where(e => e.ReturnType == GstReturnType.Gstr3B).ToList();

        var gstr2AByKey = gstr2A.ToLookup(e => e.MatchKey);
        var gstr3BByKey = gstr3B.ToLookup(e => e.MatchKey);

        var mismatches = new List<GstMismatch>();
        var matched = 0;

        foreach (var group in gstr2AByKey)
        {
            var twoA = group.First();
            var threeB = gstr3BByKey[group.Key].FirstOrDefault();

            if (threeB is null)
            {
                mismatches.Add(new GstMismatch(
                    twoA.InvoiceNumber ?? "N/A",
                    twoA.CounterpartyGstin,
                    twoA.TotalTax,
                    0,
                    "Present in GSTR-2A but missing in GSTR-3B"));
                continue;
            }

            if (Math.Abs(twoA.TotalTax - threeB.TotalTax) > 0.5m
                || Math.Abs(twoA.TaxableValue - threeB.TaxableValue) > 1m)
            {
                mismatches.Add(new GstMismatch(
                    twoA.InvoiceNumber ?? "N/A",
                    twoA.CounterpartyGstin,
                    twoA.TotalTax,
                    threeB.TotalTax,
                    "Tax/taxable value mismatch between GSTR-2A and GSTR-3B"));
            }
            else
            {
                matched++;
            }
        }

        foreach (var group in gstr3BByKey)
        {
            if (!gstr2AByKey[group.Key].Any())
            {
                var threeB = group.First();
                mismatches.Add(new GstMismatch(
                    threeB.InvoiceNumber ?? "N/A",
                    threeB.CounterpartyGstin,
                    0,
                    threeB.TotalTax,
                    "Present in GSTR-3B but missing in GSTR-2A"));
            }
        }

        var netLiability = gstr3B.Sum(e => e.TotalTax) - gstr2A.Sum(e => e.TotalTax);

        return new GstReconciliationResult(
            period,
            matched,
            mismatches.Count,
            netLiability,
            mismatches);
    }
}
