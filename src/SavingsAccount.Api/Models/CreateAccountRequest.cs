namespace SavingsAccount.Api.Models;

public class CreateAccountRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal? InterestRate { get; set; }
}