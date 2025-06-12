namespace SavingsAccount.Api.Models;

public class AccountCreationResponse
{
    public string AccountId { get; set; } = string.Empty;
    public long Version { get; set; }
}