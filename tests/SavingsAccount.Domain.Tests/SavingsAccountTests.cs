using SavingsAccount.Domain;

namespace SavingsAccount.Domain.Tests;

public class SavingsAccountTests
{
    [Fact]
    public void Constructor_CreatesAccountWithMinimumBalance()
    {
        var account = new Domain.SavingsAccount("test-123");
        
        Assert.Equal("test-123", account.Id);
        Assert.Equal(0.01m, account.Balance.Amount);
        Assert.Equal(0.042m, account.InterestRate.Value);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var account = new Domain.SavingsAccount("test-123");
        var depositAmount = new Money(100.00m);
        
        account.Deposit(depositAmount);
        
        Assert.Equal(100.01m, account.Balance.Amount);
        Assert.Single(account.Transactions);
        Assert.Equal(TransactionType.Deposit, account.Transactions[0].Type);
        Assert.Equal(2, account.Version);
    }

    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var account = new Domain.SavingsAccount("test-123");
        account.Deposit(new Money(100.00m));
        
        account.Withdraw(new Money(50.00m));
        
        Assert.Equal(50.01m, account.Balance.Amount);
        Assert.Equal(2, account.Transactions.Count);
        Assert.Equal(TransactionType.Withdrawal, account.Transactions[1].Type);
        Assert.Equal(3, account.Version);
    }

    [Fact]
    public void Withdraw_WithInsufficientFunds_ThrowsException()
    {
        var account = new Domain.SavingsAccount("test-123");
        var withdrawAmount = new Money(100.00m);
        
        Assert.Throws<InvalidOperationException>(() => account.Withdraw(withdrawAmount));
    }

    [Fact]
    public void AccrueInterest_CalculatesCorrectly()
    {
        var account = new Domain.SavingsAccount("test-123");
        account.Deposit(new Money(1000.00m));
        
        var interestEarned = account.AccrueInterest();
        
        Assert.True(interestEarned.Amount > 0);
        Assert.True(account.Balance.Amount > 1000.01m);
        Assert.Equal(TransactionType.InterestAccrual, account.Transactions.Last().Type);
        Assert.Equal(3, account.Version);
    }

    [Fact]
    public void IdempotencyKey_PreventseDuplicateTransactions()
    {
        var account = new Domain.SavingsAccount("test-123");
        var depositAmount = new Money(100.00m);
        
        account.Deposit(depositAmount, "key-123");
        account.Deposit(depositAmount, "key-123");
        
        Assert.Equal(100.01m, account.Balance.Amount);
        Assert.Single(account.Transactions);
        Assert.Equal(2, account.Version);
    }
}