namespace SavingsAccount.Api.Models;

public class WithdrawalRequest
{
    public string Amount { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}