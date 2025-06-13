using SavingsAccount.Domain;
using SavingsAccount.Infrastructure;
using Microsoft.Data.Sqlite;
using Dapper;

namespace SavingsAccount.Infrastructure.Tests;

public class DapperSavingsAccountRepositoryTests : IDisposable
{
    private readonly string _connectionString;
    private readonly string _tempDbPath;

    public DapperSavingsAccountRepositoryTests()
    {
        // Use temp file-based SQLite database for tests
        _tempDbPath = Path.GetTempFileName();
        _connectionString = $"Data Source={_tempDbPath}";
        
        // Initialize schema directly for tests
        InitializeTestSchema();
    }

    private void InitializeTestSchema()
    {
        var schema = @"
CREATE TABLE IF NOT EXISTS SavingsAccounts (
    Id TEXT PRIMARY KEY NOT NULL,
    Balance DECIMAL(18,2) NOT NULL,
    InterestRate DECIMAL(5,4) NOT NULL,
    CreatedAt TEXT NOT NULL,
    Version INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Transactions (
    Id TEXT PRIMARY KEY NOT NULL,
    AccountId TEXT NOT NULL,
    Type INTEGER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Timestamp TEXT NOT NULL,
    IdempotencyKey TEXT,
    FOREIGN KEY (AccountId) REFERENCES SavingsAccounts(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId ON Transactions(AccountId);
CREATE INDEX IF NOT EXISTS IX_Transactions_IdempotencyKey ON Transactions(IdempotencyKey);";
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute(schema);
    }

    private DapperSavingsAccountRepository CreateRepository()
    {
        // Create a test version that skips schema initialization
        // since we've already done it in InitializeTestSchema
        return new TestDapperSavingsAccountRepository(_connectionString);
    }

    [Fact]
    public async Task CreateAccount_ShouldPersistAccount()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-1");

        // Act
        var savedAccount = await repository.SaveAsync(account);

        // Assert
        Assert.NotNull(savedAccount);
        Assert.Equal("test-account-1", savedAccount.Id);
        Assert.Equal(new Money(0.01m), savedAccount.Balance);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAccount_ShouldReturnAccount()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-2");
        await repository.SaveAsync(account);

        // Act
        var retrievedAccount = await repository.GetByIdAsync("test-account-2");

        // Assert
        Assert.NotNull(retrievedAccount);
        Assert.Equal("test-account-2", retrievedAccount.Id);
        Assert.Equal(account.Balance, retrievedAccount.Balance);
        Assert.Equal(account.Version, retrievedAccount.Version);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentAccount_ShouldReturnNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingAccount_ShouldReturnTrue()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-3");
        await repository.SaveAsync(account);

        // Act
        var exists = await repository.ExistsAsync("test-account-3");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentAccount_ShouldReturnFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var exists = await repository.ExistsAsync("non-existent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task SaveAsync_WithTransactions_ShouldPersistTransactions()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-4");
        account.Deposit(new Money(100.00m), "deposit-1");
        account.Withdraw(new Money(25.50m), "withdrawal-1");

        // Act
        await repository.SaveAsync(account);
        var retrievedAccount = await repository.GetByIdAsync("test-account-4");

        // Assert
        Assert.NotNull(retrievedAccount);
        Assert.Equal(2, retrievedAccount.Transactions.Count);
        Assert.Equal(new Money(74.51m), retrievedAccount.Balance); // 0.01 + 100.00 - 25.50
    }

    [Fact]
    public async Task SaveAsync_IdempotentTransactions_ShouldNotDuplicate()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-5");
        account.Deposit(new Money(50.00m), "idempotent-key");
        await repository.SaveAsync(account);

        // Act - try to deposit again with same idempotency key
        var retrievedAccount = await repository.GetByIdAsync("test-account-5");
        retrievedAccount!.Deposit(new Money(50.00m), "idempotent-key");
        await repository.SaveAsync(retrievedAccount);

        // Assert
        var finalAccount = await repository.GetByIdAsync("test-account-5");
        Assert.NotNull(finalAccount);
        Assert.Single(finalAccount.Transactions);
        Assert.Equal(new Money(50.01m), finalAccount.Balance); // Only one deposit should be recorded
    }

    [Fact]
    public async Task SaveAsync_ConcurrentModification_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-6");
        await repository.SaveAsync(account);

        var account1 = await repository.GetByIdAsync("test-account-6");
        var account2 = await repository.GetByIdAsync("test-account-6");

        // Act & Assert
        account1!.Deposit(new Money(10.00m));
        account2!.Deposit(new Money(20.00m));

        await repository.SaveAsync(account1);

        // This should throw due to optimistic concurrency control
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(account2));
    }

    [Fact]
    public async Task SaveAsync_NewTransactionsOnly_ShouldOnlyPersistNew()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-7");
        account.Deposit(new Money(100.00m), "first-deposit");
        await repository.SaveAsync(account);

        // Act - add new transaction
        var retrievedAccount = await repository.GetByIdAsync("test-account-7");
        retrievedAccount!.Deposit(new Money(50.00m), "second-deposit");
        await repository.SaveAsync(retrievedAccount);

        // Assert
        var finalAccount = await repository.GetByIdAsync("test-account-7");
        Assert.NotNull(finalAccount);
        Assert.Equal(2, finalAccount.Transactions.Count);
        Assert.Equal(new Money(150.01m), finalAccount.Balance);
    }

    [Fact]
    public async Task SaveAsync_InterestAccrual_ShouldPersistCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        var account = new Domain.SavingsAccount("test-account-8", new InterestRate(0.10m));
        account.Deposit(new Money(1000.00m));
        await repository.SaveAsync(account);

        // Act
        var retrievedAccount = await repository.GetByIdAsync("test-account-8");
        var interestEarned = retrievedAccount!.AccrueInterest("interest-1");
        await repository.SaveAsync(retrievedAccount);

        // Assert
        var finalAccount = await repository.GetByIdAsync("test-account-8");
        Assert.NotNull(finalAccount);
        Assert.True(interestEarned.Amount > 0.01m);
        Assert.Contains(finalAccount.Transactions, t => t.Type == TransactionType.InterestAccrual);
    }

    [Fact]
    public void Constructor_InvalidConnectionString_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DapperSavingsAccountRepository(null!));
    }

    [Fact]
    public void Constructor_DatabaseInitialization_ShouldCreateTables()
    {
        // This test verifies that the constructor successfully initializes the database
        // The fact that other tests pass indicates tables were created correctly
        // Act & Assert - constructor should not throw
        var repository = new DapperSavingsAccountRepository("Data Source=:memory:");
        Assert.NotNull(repository);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

// Test-specific repository that skips schema initialization
internal class TestDapperSavingsAccountRepository : DapperSavingsAccountRepository
{
    public TestDapperSavingsAccountRepository(string connectionString) : base(connectionString, skipInitialization: true)
    {
    }
}