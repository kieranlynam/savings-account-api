namespace SavingsAccount.Domain;

public class Transaction(string id, string accountId, TransactionType type, Money amount, string? idempotencyKey = null)
{
    public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
    public string AccountId { get; } = accountId ?? throw new ArgumentNullException(nameof(accountId));
    public TransactionType Type { get; } = type;
    public Money Amount { get; } = amount;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string? IdempotencyKey { get; } = idempotencyKey;
}