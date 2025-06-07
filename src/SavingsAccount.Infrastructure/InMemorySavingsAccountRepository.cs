using SavingsAccount.Domain;
using System.Collections.Concurrent;

namespace SavingsAccount.Infrastructure;

public class InMemorySavingsAccountRepository : ISavingsAccountRepository
{
    private readonly ConcurrentDictionary<string, Domain.SavingsAccount> _accounts = new();

    public Task<Domain.SavingsAccount?> GetByIdAsync(string id)
    {
        _accounts.TryGetValue(id, out var account);
        return Task.FromResult(account);
    }

    public Task<Domain.SavingsAccount> SaveAsync(Domain.SavingsAccount account)
    {
        _accounts.AddOrUpdate(account.Id, account, (key, existing) => account);
        return Task.FromResult(account);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_accounts.ContainsKey(id));
    }
}