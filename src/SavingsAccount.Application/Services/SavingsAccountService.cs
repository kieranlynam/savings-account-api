using SavingsAccount.Domain;

namespace SavingsAccount.Application.Services;

public class SavingsAccountService(ISavingsAccountRepository repository)
{
    private readonly ISavingsAccountRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Domain.SavingsAccount> CreateAccountAsync(string accountId, decimal interestRate = 0.042m)
    {
        if (await _repository.ExistsAsync(accountId))
            throw new InvalidOperationException($"Account {accountId} already exists");

        var account = new Domain.SavingsAccount(accountId, new InterestRate(interestRate));
        return await _repository.SaveAsync(account);
    }

    public async Task<Domain.SavingsAccount> DepositAsync(string accountId, Money amount, string? idempotencyKey = null)
    {
        var account = await GetAccountInternalAsync(accountId);
        account.Deposit(amount, idempotencyKey);
        return await _repository.SaveAsync(account);
    }

    public async Task<Domain.SavingsAccount> WithdrawAsync(string accountId, Money amount, string? idempotencyKey = null)
    {
        var account = await GetAccountInternalAsync(accountId);
        account.Withdraw(amount, idempotencyKey);
        return await _repository.SaveAsync(account);
    }

    public async Task<(Domain.SavingsAccount Account, Money InterestEarned)> AccrueInterestAsync(string accountId, string? idempotencyKey = null)
    {
        var account = await GetAccountInternalAsync(accountId);
        var interestEarned = account.AccrueInterest(idempotencyKey);
        var updatedAccount = await _repository.SaveAsync(account);
        return (updatedAccount, interestEarned);
    }

    public async Task<Money> GetBalanceAsync(string accountId)
    {
        var account = await GetAccountInternalAsync(accountId);
        return account.Balance;
    }

    public async Task<Domain.SavingsAccount> GetAccountAsync(string accountId)
    {
        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");
        return account;
    }

    private async Task<Domain.SavingsAccount> GetAccountInternalAsync(string accountId)
    {
        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
            throw new InvalidOperationException($"Account {accountId} not found");
        return account;
    }
}