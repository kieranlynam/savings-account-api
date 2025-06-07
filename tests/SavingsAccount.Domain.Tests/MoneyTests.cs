using SavingsAccount.Domain;

namespace SavingsAccount.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidDecimal_CreatesMoney()
    {
        var money = new Money(123.45m);
        Assert.Equal(123.45m, money.Amount);
    }

    [Fact]
    public void Constructor_WithValidString_CreatesMoney()
    {
        var money = new Money("123.45");
        Assert.Equal(123.45m, money.Amount);
    }

    [Theory]
    [InlineData("123.4")]
    [InlineData("123")]
    [InlineData("123.456")]
    [InlineData("abc")]
    [InlineData("")]
    public void Constructor_WithInvalidString_ThrowsArgumentException(string invalidAmount)
    {
        Assert.Throws<ArgumentException>(() => new Money(invalidAmount));
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1.00m));
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Money(0.00m));
    }

    [Fact]
    public void Addition_WorksCorrectly()
    {
        var money1 = new Money(10.50m);
        var money2 = new Money(5.25m);
        var result = money1 + money2;
        
        Assert.Equal(15.75m, result.Amount);
    }

    [Fact]
    public void Subtraction_WorksCorrectly()
    {
        var money1 = new Money(10.50m);
        var money2 = new Money(5.25m);
        var result = money1 - money2;
        
        Assert.Equal(5.25m, result.Amount);
    }

    [Fact]
    public void Multiplication_WorksCorrectly()
    {
        var money = new Money(10.00m);
        var result = money * 1.5m;
        
        Assert.Equal(15.00m, result.Amount);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var money = new Money(123.45m);
        Assert.Equal("123.45", money.ToString());
    }
}