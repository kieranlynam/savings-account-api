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
    [ProducesResponseType<AccountCreationResponse>(201)]
    [ProducesResponseType<ErrorResponse>(400)]
    [ProducesResponseType<ErrorResponse>(409)]
    [ProducesResponseType<ErrorResponse>(500)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AccountId))
                return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = "AccountId is required" });

            var account = await _accountService.CreateAccountAsync(
                request.AccountId, 
                request.InterestRate ?? 0.042m);

            return Created($"/accounts/{account.Id}", new AccountCreationResponse { AccountId = account.Id });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Error = "ACCOUNT_ALREADY_EXISTS", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = ex.Message });
        }
    }

    [HttpPost("{accountId}/deposits")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ErrorResponse>(400)]
    [ProducesResponseType<ErrorResponse>(404)]
    [ProducesResponseType<ErrorResponse>(422)]
    [ProducesResponseType<ErrorResponse>(500)]
    public async Task<IActionResult> Deposit(string accountId, [FromBody] MoneyAmount request, [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            var amount = new Money(request.Amount);
            await _accountService.DepositAsync(accountId, amount, idempotencyKey);
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Error = "ACCOUNT_NOT_FOUND", Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ErrorResponse { Error = "INVALID_AMOUNT", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = ex.Message });
        }
    }

    [HttpPost("{accountId}/withdrawals")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ErrorResponse>(400)]
    [ProducesResponseType<ErrorResponse>(404)]
    [ProducesResponseType<ErrorResponse>(422)]
    [ProducesResponseType<ErrorResponse>(500)]
    public async Task<IActionResult> Withdraw(string accountId, [FromBody] MoneyAmount request, [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            var amount = new Money(request.Amount);
            await _accountService.WithdrawAsync(accountId, amount, idempotencyKey);
            
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ErrorResponse { Error = "ACCOUNT_NOT_FOUND", Message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient funds"))
        {
            return UnprocessableEntity(new ErrorResponse { Error = "INSUFFICIENT_FUNDS", Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ErrorResponse { Error = "INVALID_AMOUNT", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = ex.Message });
        }
    }

    [HttpGet("{accountId}/balance")]
    [ProducesResponseType<BalanceResponse>(200)]
    [ProducesResponseType<ErrorResponse>(400)]
    [ProducesResponseType<ErrorResponse>(404)]
    [ProducesResponseType<ErrorResponse>(500)]
    public async Task<IActionResult> GetBalance(string accountId)
    {
        try
        {
            var balance = await _accountService.GetBalanceAsync(accountId);
            return Ok(new BalanceResponse { AccountId = accountId, Balance = balance.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Error = "ACCOUNT_NOT_FOUND", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = ex.Message });
        }
    }

    [HttpPost("{accountId}/interest_accruals")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ErrorResponse>(400)]
    [ProducesResponseType<ErrorResponse>(404)]
    [ProducesResponseType<ErrorResponse>(500)]
    public async Task<IActionResult> AccrueInterest(string accountId, [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            await _accountService.AccrueInterestAsync(accountId, idempotencyKey);
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Error = "ACCOUNT_NOT_FOUND", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse { Error = "INVALID_REQUEST", Message = ex.Message });
        }
    }
}