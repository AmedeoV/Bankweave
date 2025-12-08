using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TestController> _logger;

    public TestController(AppDbContext dbContext, ILogger<TestController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("seed-mock-data")]
    public async Task<IActionResult> SeedMockData()
    {
        try
        {
            _logger.LogInformation("Seeding mock bank data...");

            // Create mock accounts
            var accounts = new List<FinancialAccount>
            {
                new FinancialAccount
                {
                    ExternalId = "mock-acc-001",
                    Provider = "TrueLayer Mock Bank",
                    DisplayName = "Personal Current Account",
                    Iban = "GB33BUKB20201555555555",
                    CurrencyCode = "EUR",
                    CurrentBalance = 5430.50m,
                    CreatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                },
                new FinancialAccount
                {
                    ExternalId = "mock-acc-002",
                    Provider = "TrueLayer Mock Bank",
                    DisplayName = "Savings Account",
                    Iban = "GB29NWBK60161331926819",
                    CurrencyCode = "EUR",
                    CurrentBalance = 12500.00m,
                    CreatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                },
                new FinancialAccount
                {
                    ExternalId = "mock-acc-003",
                    Provider = "Revolut",
                    DisplayName = "Revolut EUR",
                    Iban = "LT123456789012345678",
                    CurrencyCode = "EUR",
                    CurrentBalance = 2840.75m,
                    CreatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                }
            };

            _dbContext.FinancialAccounts.AddRange(accounts);
            await _dbContext.SaveChangesAsync();

            // Create mock transactions
            var transactions = new List<MoneyMovement>();
            var random = new Random();
            var merchants = new[] { "Tesco", "Lidl", "Amazon", "Netflix", "Spotify", "Salary", "Rent", "Utilities", "Coffee Shop", "Restaurant" };
            var categories = new[] { "Groceries", "Shopping", "Entertainment", "Income", "Housing", "Bills", "Food & Drink" };

            foreach (var account in accounts)
            {
                // Add 20 transactions per account
                for (int i = 0; i < 20; i++)
                {
                    var isIncome = random.Next(0, 10) < 2; // 20% chance of income
                    var amount = isIncome ? random.Next(1000, 3000) : -random.Next(5, 200);
                    var merchant = merchants[random.Next(merchants.Length)];
                    var category = categories[random.Next(categories.Length)];

                    var txnDate = DateTime.UtcNow.AddDays(-random.Next(1, 60));
                    transactions.Add(new MoneyMovement
                    {
                        FinancialAccountId = account.Id,
                        TransactionId = $"txn-{account.ExternalId}-{i:D3}",
                        TransactionDate = txnDate,
                        BookingDate = txnDate,
                        Description = isIncome ? $"Payment from {merchant}" : $"Payment to {merchant}",
                        Amount = amount,
                        CurrencyCode = account.CurrencyCode,
                        Category = category,
                        CounterpartyName = merchant,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _dbContext.MoneyMovements.AddRange(transactions);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Mock data seeded successfully");

            return Ok(new
            {
                message = "Mock data seeded successfully",
                accountsCreated = accounts.Count,
                transactionsCreated = transactions.Count,
                accounts = accounts.Select(a => new
                {
                    id = a.Id,
                    provider = a.Provider,
                    displayName = a.DisplayName,
                    balance = a.CurrentBalance,
                    currency = a.CurrencyCode,
                    transactionCount = transactions.Count(t => t.FinancialAccountId == a.Id)
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed mock data");
            return StatusCode(500, new { error = "Failed to seed mock data", details = ex.Message });
        }
    }

    [HttpDelete("clear-data")]
    public async Task<IActionResult> ClearData()
    {
        try
        {
            _logger.LogInformation("Clearing all data...");

            _dbContext.MoneyMovements.RemoveRange(_dbContext.MoneyMovements);
            _dbContext.FinancialAccounts.RemoveRange(_dbContext.FinancialAccounts);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("All data cleared");

            return Ok(new { message = "All data cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear data");
            return StatusCode(500, new { error = "Failed to clear data", details = ex.Message });
        }
    }
}
