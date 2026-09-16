using FluentAssertions;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_Rounds_To_Two_Decimals()
    {
        var money = new Money(10.456m);
        money.Amount.Should().Be(10.46m);
        money.Currency.Should().Be("INR");
    }

    [Fact]
    public void Constructor_Rejects_Negative()
    {
        var act = () => new Money(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Add_Same_Currency_Works()
    {
        var sum = new Money(10).Add(new Money(5.5m));
        sum.Amount.Should().Be(15.5m);
    }

    [Fact]
    public void Add_Different_Currency_Throws()
    {
        var act = () => new Money(10, "INR").Add(new Money(5, "USD"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Zero_Factory()
    {
        Money.Zero().Amount.Should().Be(0);
    }
}
