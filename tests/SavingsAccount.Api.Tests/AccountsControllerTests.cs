using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SavingsAccount.Api.Models;

namespace SavingsAccount.Api.Tests;

public class AccountsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AccountsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task CreateAccount_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            AccountId = "test_account_001",
            InterestRate = 0.042m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var accountResponse = JsonSerializer.Deserialize<AccountCreationResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(accountResponse);
        Assert.Equal("test_account_001", accountResponse.AccountId);
    }

    [Fact]
    public async Task CreateAccount_WithMissingAccountId_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            AccountId = "",
            InterestRate = 0.042m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("INVALID_REQUEST", errorResponse.Error);
        Assert.Contains("AccountId is required", errorResponse.Message);
    }

    [Fact]
    public async Task CreateAccount_WithDuplicateId_Returns409Conflict()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            AccountId = "duplicate_account",
            InterestRate = 0.042m
        };

        // Act - Create first account
        await _client.PostAsJsonAsync("/accounts", request, _jsonOptions);
        
        // Act - Try to create duplicate
        var response = await _client.PostAsJsonAsync("/accounts", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("ACCOUNT_ALREADY_EXISTS", errorResponse.Error);
    }

    [Fact]
    public async Task GetBalance_WithValidAccount_Returns200Ok()
    {
        // Arrange
        var accountId = "balance_test_account";
        await CreateTestAccount(accountId);

        // Act
        var response = await _client.GetAsync($"/accounts/{accountId}/balance");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var balanceResponse = JsonSerializer.Deserialize<BalanceResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(balanceResponse);
        Assert.Equal(accountId, balanceResponse.AccountId);
        Assert.Equal("0.01", balanceResponse.Balance); // Initial balance
    }

    [Fact]
    public async Task GetBalance_WithNonExistentAccount_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/accounts/non_existent_account/balance");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("ACCOUNT_NOT_FOUND", errorResponse.Error);
    }

    [Fact]
    public async Task Deposit_WithValidAmount_Returns204NoContent()
    {
        // Arrange
        var accountId = "deposit_test_account";
        await CreateTestAccount(accountId);
        
        var depositRequest = new MoneyAmount { Amount = "100.50" };

        // Act
        var response = await _client.PostAsJsonAsync($"/accounts/{accountId}/deposits", depositRequest, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify balance updated
        var balanceResponse = await _client.GetAsync($"/accounts/{accountId}/balance");
        var balanceContent = await balanceResponse.Content.ReadAsStringAsync();
        var balance = JsonSerializer.Deserialize<BalanceResponse>(balanceContent, _jsonOptions);
        
        Assert.Equal("100.51", balance?.Balance); // 100.50 + 0.01 initial
    }

    [Fact]
    public async Task Deposit_WithInvalidAmount_Returns422UnprocessableEntity()
    {
        // Arrange
        var accountId = "invalid_deposit_account";
        await CreateTestAccount(accountId);
        
        var depositRequest = new MoneyAmount { Amount = "invalid_amount" };

        // Act
        var response = await _client.PostAsJsonAsync($"/accounts/{accountId}/deposits", depositRequest, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("INVALID_AMOUNT", errorResponse.Error);
    }

    [Fact]
    public async Task Withdraw_WithValidAmount_Returns204NoContent()
    {
        // Arrange
        var accountId = "withdraw_test_account";
        await CreateTestAccount(accountId);
        
        // First deposit some money
        var depositRequest = new MoneyAmount { Amount = "100.00" };
        await _client.PostAsJsonAsync($"/accounts/{accountId}/deposits", depositRequest, _jsonOptions);
        
        var withdrawRequest = new MoneyAmount { Amount = "50.00" };

        // Act
        var response = await _client.PostAsJsonAsync($"/accounts/{accountId}/withdrawals", withdrawRequest, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify balance updated
        var balanceResponse = await _client.GetAsync($"/accounts/{accountId}/balance");
        var balanceContent = await balanceResponse.Content.ReadAsStringAsync();
        var balance = JsonSerializer.Deserialize<BalanceResponse>(balanceContent, _jsonOptions);
        
        Assert.Equal("50.01", balance?.Balance); // 100.01 - 50.00
    }

    [Fact]
    public async Task Withdraw_WithInsufficientFunds_Returns422UnprocessableEntity()
    {
        // Arrange
        var accountId = "insufficient_funds_account";
        await CreateTestAccount(accountId);
        
        var withdrawRequest = new MoneyAmount { Amount = "100.00" }; // More than 0.01 initial balance

        // Act
        var response = await _client.PostAsJsonAsync($"/accounts/{accountId}/withdrawals", withdrawRequest, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("INSUFFICIENT_FUNDS", errorResponse.Error);
    }

    [Fact]
    public async Task AccrueInterest_WithValidAccount_Returns204NoContent()
    {
        // Arrange
        var accountId = "interest_test_account";
        await CreateTestAccount(accountId);
        
        // Add some balance first
        var depositRequest = new MoneyAmount { Amount = "1000.00" };
        await _client.PostAsJsonAsync($"/accounts/{accountId}/deposits", depositRequest, _jsonOptions);

        // Act
        var response = await _client.PostAsync($"/accounts/{accountId}/interest_accruals", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AccrueInterest_WithNonExistentAccount_Returns404NotFound()
    {
        // Act
        var response = await _client.PostAsync("/accounts/non_existent_account/interest_accruals", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.Equal("ACCOUNT_NOT_FOUND", errorResponse.Error);
    }

    [Fact]
    public async Task EndToEndWorkflow_CreateDepositWithdrawBalance_WorksCorrectly()
    {
        // Arrange
        var accountId = "e2e_test_account";
        
        // Act & Assert - Create Account
        var createRequest = new CreateAccountRequest { AccountId = accountId, InterestRate = 0.05m };
        var createResponse = await _client.PostAsJsonAsync("/accounts", createRequest, _jsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        // Act & Assert - Initial Balance
        var initialBalanceResponse = await _client.GetAsync($"/accounts/{accountId}/balance");
        var initialBalance = await GetBalanceFromResponse(initialBalanceResponse);
        Assert.Equal("0.01", initialBalance);
        
        // Act & Assert - Deposit
        var depositRequest = new MoneyAmount { Amount = "250.75" };
        var depositResponse = await _client.PostAsJsonAsync($"/accounts/{accountId}/deposits", depositRequest, _jsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, depositResponse.StatusCode);
        
        // Act & Assert - Balance After Deposit
        var afterDepositResponse = await _client.GetAsync($"/accounts/{accountId}/balance");
        var afterDepositBalance = await GetBalanceFromResponse(afterDepositResponse);
        Assert.Equal("250.76", afterDepositBalance);
        
        // Act & Assert - Withdrawal
        var withdrawRequest = new MoneyAmount { Amount = "75.25" };
        var withdrawResponse = await _client.PostAsJsonAsync($"/accounts/{accountId}/withdrawals", withdrawRequest, _jsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, withdrawResponse.StatusCode);
        
        // Act & Assert - Final Balance
        var finalBalanceResponse = await _client.GetAsync($"/accounts/{accountId}/balance");
        var finalBalance = await GetBalanceFromResponse(finalBalanceResponse);
        Assert.Equal("175.51", finalBalance);
    }

    private async Task CreateTestAccount(string accountId)
    {
        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            InterestRate = 0.042m
        };
        
        await _client.PostAsJsonAsync("/accounts", request, _jsonOptions);
    }

    private async Task<string> GetBalanceFromResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var balanceResponse = JsonSerializer.Deserialize<BalanceResponse>(content, _jsonOptions);
        return balanceResponse?.Balance ?? "";
    }
}