using SavingsAccount.Application.Services;
using SavingsAccount.Domain;
using SavingsAccount.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ISavingsAccountRepository, InMemorySavingsAccountRepository>();
builder.Services.AddScoped<SavingsAccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
