using Dapper;
using Microsoft.Data.Sqlite;
using SavingsAccount.Domain;
using System.Data;

namespace SavingsAccount.Infrastructure;

public class DapperSavingsAccountRepository : ISavingsAccountRepository
{
    private readonly string _connectionString;

    public DapperSavingsAccountRepository(string connectionString) : this(connectionString, skipInitialization: false)
    {
    }

    protected DapperSavingsAccountRepository(string connectionString, bool skipInitialization)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        
        if (!skipInitialization)
        {
            try
            {
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize database. Check connection string and file permissions.", ex);
            }
        }
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        connection.Execute(schema);
    }

    public async Task<Domain.SavingsAccount?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Check if account exists first
        const string existsSql = "SELECT COUNT(1) FROM SavingsAccounts WHERE Id = @id";
        var exists = await connection.QuerySingleAsync<int>(existsSql, new { id }) > 0;
        if (!exists) return null;

        // Get account metadata for reconstruction
        const string accountSql = @"
            SELECT Id, InterestRate 
            FROM SavingsAccounts 
            WHERE Id = @id";

        const string transactionsSql = @"
            SELECT Id, AccountId, Type, Amount, Timestamp, IdempotencyKey 
            FROM Transactions 
            WHERE AccountId = @id 
            ORDER BY Timestamp";

        var accountData = await connection.QuerySingleOrDefaultAsync(accountSql, new { id });
        var transactions = await connection.QueryAsync(transactionsSql, new { id });

        // Reconstruct the account from scratch with correct interest rate
        var account = new Domain.SavingsAccount(
            accountData.Id,
            new InterestRate((decimal)accountData.InterestRate));

        // Replay all transactions to rebuild state (this is pure event sourcing)
        foreach (var tx in transactions)
        {
            var amount = new Money((decimal)tx.Amount);
            var type = (TransactionType)(int)tx.Type;
            var idempotencyKey = tx.IdempotencyKey as string;

            switch (type)
            {
                case TransactionType.Deposit:
                    account.Deposit(amount, idempotencyKey);
                    break;
                case TransactionType.Withdrawal:
                    account.Withdraw(amount, idempotencyKey);
                    break;
                case TransactionType.InterestAccrual:
                    account.AccrueInterest(idempotencyKey);
                    break;
            }
        }

        return account;
    }

    public async Task<Domain.SavingsAccount> SaveAsync(Domain.SavingsAccount account)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Check if account exists
            const string existsSql = "SELECT COUNT(1) FROM SavingsAccounts WHERE Id = @id";
            var exists = await connection.QuerySingleAsync<int>(existsSql, new { id = account.Id }, transaction) > 0;

            if (!exists)
            {
                // Insert new account (only metadata, not derived state)
                const string insertAccountSql = @"
                    INSERT INTO SavingsAccounts (Id, Balance, InterestRate, CreatedAt, Version)
                    VALUES (@Id, @Balance, @InterestRate, @CreatedAt, @Version)";

                await connection.ExecuteAsync(insertAccountSql, new
                {
                    account.Id,
                    Balance = account.Balance.Amount,
                    InterestRate = account.InterestRate.Value,
                    CreatedAt = account.CreatedAt.ToString("O"),
                    account.Version
                }, transaction);
            }

            // Insert new transactions (only those not already persisted)
            const string maxTransactionTimestampSql = @"
                SELECT COALESCE(MAX(Timestamp), '1900-01-01T00:00:00.0000000Z') 
                FROM Transactions 
                WHERE AccountId = @accountId";

            var lastPersistedTimestamp = await connection.QuerySingleAsync<string>(
                maxTransactionTimestampSql, 
                new { accountId = account.Id }, 
                transaction);

            var lastPersisted = DateTime.Parse(lastPersistedTimestamp);
            var newTransactions = account.Transactions.Where(t => t.Timestamp > lastPersisted).ToList();

            // Only update account if there are new transactions (prevents unnecessary concurrency conflicts)
            if (newTransactions.Any())
            {
                // Update account state since there are new transactions
                if (exists)
                {
                    // Get current persisted version for concurrency check
                    const string currentVersionSql = "SELECT Version FROM SavingsAccounts WHERE Id = @id";
                    var currentVersion = await connection.QuerySingleAsync<long>(currentVersionSql, new { id = account.Id }, transaction);
                    
                    const string updateAccountSql = @"
                        UPDATE SavingsAccounts 
                        SET Balance = @Balance, InterestRate = @InterestRate, Version = @NewVersion
                        WHERE Id = @Id AND Version = @CurrentVersion";

                    var rowsAffected = await connection.ExecuteAsync(updateAccountSql, new
                    {
                        account.Id,
                        Balance = account.Balance.Amount,
                        InterestRate = account.InterestRate.Value,
                        NewVersion = account.Version,
                        CurrentVersion = currentVersion
                    }, transaction);

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("The account was modified by another process. Please retry the operation.");
                    }
                }

                // Insert the new transactions
                const string insertTransactionSql = @"
                    INSERT INTO Transactions (Id, AccountId, Type, Amount, Timestamp, IdempotencyKey)
                    VALUES (@Id, @AccountId, @Type, @Amount, @Timestamp, @IdempotencyKey)";

                foreach (var tx in newTransactions)
                {
                    await connection.ExecuteAsync(insertTransactionSql, new
                    {
                        tx.Id,
                        tx.AccountId,
                        Type = (int)tx.Type,
                        Amount = tx.Amount.Amount,
                        Timestamp = tx.Timestamp.ToString("O"),
                        tx.IdempotencyKey
                    }, transaction);
                }
            }

            transaction.Commit();
            return account;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        const string sql = "SELECT COUNT(1) FROM SavingsAccounts WHERE Id = @id";
        var count = await connection.QuerySingleAsync<int>(sql, new { id });
        
        return count > 0;
    }
}