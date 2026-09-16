using System.Text.RegularExpressions;

namespace SmartLedger.Domain.ValueObjects;

public sealed partial record GstNumber
{
    public string Value { get; }

    public GstNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("GST number is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (!GstRegex().IsMatch(normalized))
            throw new ArgumentException($"Invalid GSTIN format: {value}", nameof(value));

        Value = normalized;
    }

    public string StateCode => Value[..2];

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")]
    private static partial Regex GstRegex();
}
