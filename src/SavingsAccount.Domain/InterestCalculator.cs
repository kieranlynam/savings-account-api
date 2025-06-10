namespace SavingsAccount.Domain;

public static class InterestCalculator
{
    public static Money CalculateCompoundInterest(Money principal, InterestRate annualRate, int compoundingPeriods = 1)
    {
        if (compoundingPeriods <= 0)
            throw new ArgumentException("Compounding periods must be positive", nameof(compoundingPeriods));

        var rate = (double)(annualRate.Value / compoundingPeriods);
        var amount = (double)principal.Amount * Math.Pow(1 + rate, compoundingPeriods);
        
        return new Money((decimal)Math.Round(amount, 2));
    }
}