namespace SavingsAccount.Domain;

public interface ISavingsAccountRepository
{
    Task<SavingsAccount?> GetByIdAsync(string id);
    Task<SavingsAccount> SaveAsync(SavingsAccount account);
    Task<bool> ExistsAsync(string id);
}