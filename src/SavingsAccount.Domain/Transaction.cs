namespace SavingsAccount.Domain;

public class Transaction
{
    public string Id { get; }
    public string AccountId { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }
    public DateTime Timestamp { get; }
    public string? IdempotencyKey { get; }

    public Transaction(string id, string accountId, TransactionType type, Money amount, string? idempotencyKey = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        Type = type;
        Amount = amount;
        Timestamp = DateTime.UtcNow;
        IdempotencyKey = idempotencyKey;
    }
}