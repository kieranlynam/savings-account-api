using Microsoft.AspNetCore.Mvc;
using SavingsAccount.Api.Models;
using SavingsAccount.Application.Services;
using SavingsAccount.Domain;

namespace SavingsAccount.Api.Controllers;

[ApiController]
[Route("accounts")]
public class AccountsController : ControllerBase
{
    private readonly SavingsAccountService _accountService;

    public AccountsController(SavingsAccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AccountId))
                return BadRequest("AccountId is required");

            var account = await _accountService.CreateAccountAsync(
                request.AccountId, 
                request.InterestRate ?? 0.042m);

            return Ok(new { AccountId = account.Id, Balance = account.Balance.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/deposits")]
    public async Task<IActionResult> Deposit(string id, [FromBody] DepositRequest request)
    {
        try
        {
            var amount = new Money(request.Amount);
            var account = await _accountService.DepositAsync(id, amount, request.IdempotencyKey);
            
            return Ok(new { Balance = account.Balance.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/withdrawals")]
    public async Task<IActionResult> Withdraw(string id, [FromBody] WithdrawalRequest request)
    {
        try
        {
            var amount = new Money(request.Amount);
            var account = await _accountService.WithdrawAsync(id, amount, request.IdempotencyKey);
            
            return Ok(new { Balance = account.Balance.ToString() });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient funds"))
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/balance")]
    public async Task<IActionResult> GetBalance(string id)
    {
        try
        {
            var balance = await _accountService.GetBalanceAsync(id);
            return Ok(new BalanceResponse { Balance = balance.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/interest_accruals")]
    public async Task<IActionResult> AccrueInterest(string id, [FromBody] InterestAccrualRequest request)
    {
        try
        {
            var (account, interestEarned) = await _accountService.AccrueInterestAsync(id, request.IdempotencyKey);
            
            return Ok(new 
            { 
                Balance = account.Balance.ToString(),
                InterestEarned = interestEarned.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}