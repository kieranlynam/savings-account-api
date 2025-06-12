namespace SavingsAccount.Api.Models;

public class BalanceResponse
{
    public string AccountId { get; set; } = string.Empty;
    public string Balance { get; set; } = string.Empty;
    public long Version { get; set; }
}