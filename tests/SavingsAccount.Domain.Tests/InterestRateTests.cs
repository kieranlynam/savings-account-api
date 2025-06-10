using SavingsAccount.Domain;

namespace SavingsAccount.Domain.Tests;

public class InterestRateTests
{
    [Fact]
    public void Constructor_WithValidRate_CreatesInterestRate()
    {
        var rate = new InterestRate(0.042m);
        
        Assert.Equal(0.042m, rate.Value);
    }

    [Fact]
    public void Constructor_WithZeroRate_CreatesInterestRate()
    {
        var rate = new InterestRate(0.00m);
        
        Assert.Equal(0.00m, rate.Value);
    }

    [Fact]
    public void Constructor_WithMaximumRate_CreatesInterestRate()
    {
        var rate = new InterestRate(1.00m);
        
        Assert.Equal(1.00m, rate.Value);
    }

    [Fact]
    public void Constructor_WithNegativeRate_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new InterestRate(-0.01m));
    }

    [Fact]
    public void Constructor_WithRateAbove100Percent_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new InterestRate(1.5m));
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ReturnsValue()
    {
        var rate = new InterestRate(0.042m);
        decimal value = rate;
        
        Assert.Equal(0.042m, value);
    }

    [Fact]
    public void ExplicitConversion_FromDecimal_CreatesInterestRate()
    {
        var rate = (InterestRate)0.042m;
        
        Assert.Equal(0.042m, rate.Value);
    }

    [Fact]
    public void ToString_FormatsAsPercentage()
    {
        var rate = new InterestRate(0.042m);
        
        Assert.Equal("4.20%", rate.ToString());
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var rate1 = new InterestRate(0.042m);
        var rate2 = new InterestRate(0.042m);
        
        Assert.True(rate1.Equals(rate2));
        Assert.True(rate1 == rate2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        var rate1 = new InterestRate(0.042m);
        var rate2 = new InterestRate(0.050m);
        
        Assert.False(rate1.Equals(rate2));
        Assert.True(rate1 != rate2);
    }
}