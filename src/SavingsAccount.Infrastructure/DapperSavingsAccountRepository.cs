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
            var accountExists = await connection.QuerySingleAsync<int>(existsSql, new { id = account.Id }, transaction) > 0;

            if (!accountExists)
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
            // Get existing transaction details to check for duplicates
            const string existingTransactionsSql = @"
                SELECT Id, IdempotencyKey, Amount, Type, Timestamp 
                FROM Transactions 
                WHERE AccountId = @accountId 
                ORDER BY Timestamp";

            var existingTransactions = await connection.QueryAsync(
                existingTransactionsSql, 
                new { accountId = account.Id }, 
                transaction);

            // Filter out transactions that match existing ones by idempotency key and properties
            var existingSet = new HashSet<string>();
            foreach (var existing in existingTransactions)
            {
                var key = $"{existing.IdempotencyKey}:{existing.Amount}:{existing.Type}";
                existingSet.Add(key);
            }

            var newTransactions = account.Transactions.Where(t => 
            {
                var key = $"{t.IdempotencyKey}:{t.Amount.Amount}:{(int)t.Type}";
                return !existingSet.Contains(key);
            }).ToList();

            // Only update account if there are new transactions (prevents unnecessary concurrency conflicts)
            if (newTransactions.Count != 0)
            {
                // Update account state since there are new transactions
                if (accountExists)
                {
                    // Calculate expected database version (total transactions + 1 - new transactions)
                    var expectedVersion = account.Transactions.Count + 1 - newTransactions.Count;
                    
                    const string updateAccountSql = @"
                        UPDATE SavingsAccounts 
                        SET Balance = @Balance, InterestRate = @InterestRate, Version = @NewVersion
                        WHERE Id = @Id AND Version = @ExpectedVersion";

                    var rowsAffected = await connection.ExecuteAsync(updateAccountSql, new
                    {
                        account.Id,
                        Balance = account.Balance.Amount,
                        InterestRate = account.InterestRate.Value,
                        NewVersion = account.Version,
                        ExpectedVersion = expectedVersion
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