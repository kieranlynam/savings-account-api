using System.Text.RegularExpressions;

namespace SavingsAccount.Domain;

public readonly struct Money : IEquatable<Money>
{
    private static readonly Regex MoneyPattern = new(@"^\d+\.\d{2}$", RegexOptions.Compiled);
    
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0.01m)
            throw new ArgumentException("Amount must be at least $0.01", nameof(amount));
        
        Amount = Math.Round(amount, 2);
    }

    public Money(string amount)
    {
        if (string.IsNullOrWhiteSpace(amount))
            throw new ArgumentException("Amount cannot be null or empty", nameof(amount));
        
        if (!MoneyPattern.IsMatch(amount))
            throw new ArgumentException("Amount must be in format '123.45'", nameof(amount));
        
        if (!decimal.TryParse(amount, out var parsed))
            throw new ArgumentException("Invalid decimal format", nameof(amount));
        
        if (parsed < 0.01m)
            throw new ArgumentException("Amount must be at least $0.01", nameof(amount));
        
        Amount = parsed;
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);
    public static Money operator *(Money money, decimal multiplier) => new(money.Amount * multiplier);
    
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public bool Equals(Money other) => Amount == other.Amount;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => Amount.GetHashCode();
    public override string ToString() => Amount.ToString("F2");
    
    public static bool operator ==(Money left, Money right) => left.Equals(right);
    public static bool operator !=(Money left, Money right) => !left.Equals(right);
}