using FluentAssertions;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Tests.Domain;

public class GstNumberTests
{
    [Theory]
    [InlineData("27AABCR1234A1Z5")]
    [InlineData("29AABCD1234G1Z7")]
    public void Valid_Gstin_Accepted(string value)
    {
        var gst = new GstNumber(value);
        gst.Value.Should().Be(value.ToUpperInvariant());
        gst.StateCode.Should().Be(value[..2]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("27AABCR1234A1Z")]
    public void Invalid_Gstin_Rejected(string value)
    {
        var act = () => new GstNumber(value);
        act.Should().Throw<ArgumentException>();
    }
}
