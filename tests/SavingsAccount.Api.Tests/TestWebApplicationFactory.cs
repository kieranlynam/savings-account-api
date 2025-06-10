using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SavingsAccount.Domain;
using SavingsAccount.Infrastructure;

namespace SavingsAccount.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Use a fresh in-memory repository for each test
            services.AddSingleton<ISavingsAccountRepository, InMemorySavingsAccountRepository>();
        });

        builder.UseEnvironment("Testing");
    }
}