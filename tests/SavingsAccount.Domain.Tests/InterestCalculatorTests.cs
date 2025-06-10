using SavingsAccount.Domain;

namespace SavingsAccount.Domain.Tests;

public class InterestCalculatorTests
{
    [Fact]
    public void CalculateCompoundInterest_WithStandardRate_ReturnsCorrectAmount()
    {
        var principal = new Money(1000.00m);
        var rate = 0.042m;
        
        var result = InterestCalculator.CalculateCompoundInterest(principal, new InterestRate(rate));
        
        Assert.Equal(1042.00m, result.Amount);
    }

    [Fact]
    public void CalculateCompoundInterest_WithZeroRate_ReturnsPrincipal()
    {
        var principal = new Money(1000.00m);
        var rate = 0.00m;
        
        var result = InterestCalculator.CalculateCompoundInterest(principal, new InterestRate(rate));
        
        Assert.Equal(1000.00m, result.Amount);
    }


    [Fact]
    public void CalculateCompoundInterest_WithMultiplePeriods_ReturnsCorrectAmount()
    {
        var principal = new Money(1000.00m);
        var rate = 0.12m;
        var periods = 12;
        
        var result = InterestCalculator.CalculateCompoundInterest(principal, new InterestRate(rate), periods);
        
        Assert.True(result.Amount > principal.Amount);
    }

    [Fact]
    public void CalculateCompoundInterest_WithZeroCompoundingPeriods_ThrowsException()
    {
        var principal = new Money(1000.00m);
        var rate = new InterestRate(0.042m);
        
        Assert.Throws<ArgumentException>(() => 
            InterestCalculator.CalculateCompoundInterest(principal, rate, 0));
    }

    [Fact]
    public void CalculateCompoundInterest_WithNegativeCompoundingPeriods_ThrowsException()
    {
        var principal = new Money(1000.00m);
        var rate = new InterestRate(0.042m);
        
        Assert.Throws<ArgumentException>(() => 
            InterestCalculator.CalculateCompoundInterest(principal, rate, -1));
    }
}