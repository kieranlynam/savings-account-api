namespace SavingsAccount.Domain;

public readonly struct InterestRate : IEquatable<InterestRate>
{
    public decimal Value { get; }

    public InterestRate(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Interest rate cannot be negative", nameof(value));
        
        if (value > 1)
            throw new ArgumentException("Interest rate cannot exceed 100%", nameof(value));

        Value = value;
    }

    public static implicit operator decimal(InterestRate interestRate) => interestRate.Value;
    
    public static explicit operator InterestRate(decimal value) => new(value);

    public bool Equals(InterestRate other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is InterestRate other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"{Value:P2}";

    public static bool operator ==(InterestRate left, InterestRate right) => left.Equals(right);

    public static bool operator !=(InterestRate left, InterestRate right) => !left.Equals(right);
}