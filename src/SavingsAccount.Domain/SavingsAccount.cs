namespace SavingsAccount.Domain;

public class SavingsAccount
{
    private readonly List<Transaction> _transactions = new();
    
    public string Id { get; }
    public Money Balance { get; private set; }
    public InterestRate InterestRate { get; private set; }
    public DateTime CreatedAt { get; }
    public long Version { get; private set; }
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    public SavingsAccount(string id, InterestRate? interestRate = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        InterestRate = interestRate ?? new InterestRate(0.042m);
        Balance = new Money(0.01m);
        CreatedAt = DateTime.UtcNow;
        Version = 1;
    }

    public void Deposit(Money amount, string? idempotencyKey = null)
    {
        if (HasDuplicateTransaction(idempotencyKey))
            return;

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            Id,
            TransactionType.Deposit,
            amount,
            idempotencyKey);

        _transactions.Add(transaction);
        Balance += amount;
        Version++;
    }

    public void Withdraw(Money amount, string? idempotencyKey = null)
    {
        if (HasDuplicateTransaction(idempotencyKey))
            return;

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds");

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            Id,
            TransactionType.Withdrawal,
            amount,
            idempotencyKey);

        _transactions.Add(transaction);
        Balance -= amount;
        Version++;
    }

    public Money AccrueInterest(string? idempotencyKey = null)
    {
        if (HasDuplicateTransaction(idempotencyKey))
            return new Money(0.01m);

        var newBalance = InterestCalculator.CalculateCompoundInterest(Balance, InterestRate);
        var interestEarned = newBalance - Balance;

        if (interestEarned.Amount > 0.01m)
        {
            var transaction = new Transaction(
                Guid.NewGuid().ToString(),
                Id,
                TransactionType.InterestAccrual,
                interestEarned,
                idempotencyKey);

            _transactions.Add(transaction);
            Balance = newBalance;
            Version++;
        }

        return interestEarned;
    }

    private bool HasDuplicateTransaction(string? idempotencyKey)
    {
        return !string.IsNullOrEmpty(idempotencyKey) && 
               _transactions.Any(t => t.IdempotencyKey == idempotencyKey);
    }
}