namespace SavingsAccount.Domain;

public static class InterestCalculator
{
    public static Money CalculateCompoundInterest(Money principal, decimal annualRate, int compoundingPeriods = 1)
    {
        if (annualRate < 0)
            throw new ArgumentException("Interest rate cannot be negative", nameof(annualRate));
        
        if (compoundingPeriods <= 0)
            throw new ArgumentException("Compounding periods must be positive", nameof(compoundingPeriods));

        var rate = (double)(annualRate / compoundingPeriods);
        var amount = (double)principal.Amount * Math.Pow(1 + rate, compoundingPeriods);
        
        return new Money((decimal)Math.Round(amount, 2));
    }
}