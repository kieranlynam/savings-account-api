namespace SavingsAccount.Api.Models;

public class DepositRequest
{
    public string Amount { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}