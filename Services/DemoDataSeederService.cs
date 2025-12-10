using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Bankweave.Services;

public class DemoDataSeederService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoDataSeederService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<(bool Success, string Message, string? UserId)> CreateDemoAccountAsync()
    {
        const string demoEmail = "demo@bankweave.app";
        const string demoPassword = "Demo123!";

        // Check if demo user already exists
        var existingUser = await _userManager.FindByEmailAsync(demoEmail);
        if (existingUser != null)
        {
            // Delete existing demo user and all their data
            await DeleteDemoAccountAsync(existingUser.Id);
        }

        // Create demo user
        var demoUser = new ApplicationUser
        {
            UserName = demoEmail,
            Email = demoEmail,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(demoUser, demoPassword);
        if (!result.Succeeded)
        {
            return (false, $"Failed to create demo user: {string.Join(", ", result.Errors.Select(e => e.Description))}", null);
        }

        // Create sample financial accounts
        var accounts = CreateSampleAccounts(demoUser.Id);
        await _context.FinancialAccounts.AddRangeAsync(accounts);
        await _context.SaveChangesAsync();

        // Create sample transactions for each account
        var transactions = CreateSampleTransactions(accounts);
        await _context.MoneyMovements.AddRangeAsync(transactions);
        
        // Create sample categorization rules
        var rules = CreateSampleCategorizationRules(demoUser.Id);
        await _context.CategorizationRules.AddRangeAsync(rules);

        await _context.SaveChangesAsync();

        // Update account balances based on transactions
        UpdateAccountBalances(accounts, transactions);
        await _context.SaveChangesAsync();

        return (true, $"Demo account created successfully! Email: {demoEmail}, Password: {demoPassword}", demoUser.Id);
    }

    private async Task DeleteDemoAccountAsync(string userId)
    {
        // Delete all related data
        var accounts = _context.FinancialAccounts.Where(a => a.UserId == userId);
        var accountIds = accounts.Select(a => a.Id).ToList();
        
        var movements = _context.MoneyMovements.Where(m => accountIds.Contains(m.FinancialAccountId));
        _context.MoneyMovements.RemoveRange(movements);
        
        var rules = _context.CategorizationRules.Where(r => r.UserId == userId);
        _context.CategorizationRules.RemoveRange(rules);
        
        var scenarios = _context.WhatIfScenarios.Where(s => s.UserId == userId);
        _context.WhatIfScenarios.RemoveRange(scenarios);
        
        _context.FinancialAccounts.RemoveRange(accounts);
        
        await _context.SaveChangesAsync();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    private List<FinancialAccount> CreateSampleAccounts(string userId)
    {
        var now = DateTime.UtcNow;
        
        return new List<FinancialAccount>
        {
            new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "Demo Bank",
                ExternalId = "DEMO_CHECKING_001",
                DisplayName = "Main Checking Account",
                CurrencyCode = "EUR",
                CurrentBalance = 0, // Will be calculated
                StartingBalance = 5000.00m,
                ExcludeFromTotal = false,
                IsCreditCard = false,
                CreatedAt = now.AddMonths(-6),
                LastSyncedAt = now
            },
            new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "Demo Bank",
                ExternalId = "DEMO_SAVINGS_001",
                DisplayName = "Savings Account",
                CurrencyCode = "EUR",
                CurrentBalance = 0, // Will be calculated
                StartingBalance = 15000.00m,
                ExcludeFromTotal = false,
                IsCreditCard = false,
                CreatedAt = now.AddMonths(-6),
                LastSyncedAt = now
            },
            new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "Demo Credit Card",
                ExternalId = "DEMO_CC_001",
                DisplayName = "Visa Credit Card",
                CurrencyCode = "EUR",
                CurrentBalance = 0, // Will be calculated
                StartingBalance = 0m,
                ExcludeFromTotal = false,
                IsCreditCard = true,
                CreatedAt = now.AddMonths(-6),
                LastSyncedAt = now
            },
            new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "Trading212",
                ExternalId = "DEMO_INVEST_001",
                DisplayName = "Investment Account",
                CurrencyCode = "EUR",
                CurrentBalance = 0, // Will be calculated
                StartingBalance = 10000.00m,
                ExcludeFromTotal = false,
                IsCreditCard = false,
                CreatedAt = now.AddMonths(-6),
                LastSyncedAt = now
            }
        };
    }

    private List<MoneyMovement> CreateSampleTransactions(List<FinancialAccount> accounts)
    {
        var transactions = new List<MoneyMovement>();
        var now = DateTime.UtcNow;
        var checking = accounts[0];
        var savings = accounts[1];
        var creditCard = accounts[2];
        var investment = accounts[3];

        // Checking Account Transactions (last 3 months)
        var checkingTransactions = new[]
        {
            // Salary
            (Date: now.AddDays(-85), Amount: 3500.00m, Description: "Salary - Tech Company Inc", Counterparty: "Tech Company Inc", Category: "Income", Essential: false),
            (Date: now.AddDays(-55), Amount: 3500.00m, Description: "Salary - Tech Company Inc", Counterparty: "Tech Company Inc", Category: "Income", Essential: false),
            (Date: now.AddDays(-25), Amount: 3500.00m, Description: "Salary - Tech Company Inc", Counterparty: "Tech Company Inc", Category: "Income", Essential: false),
            
            // Rent
            (Date: now.AddDays(-82), Amount: -1200.00m, Description: "Monthly Rent Payment", Counterparty: "City Properties LLC", Category: "Housing", Essential: true),
            (Date: now.AddDays(-52), Amount: -1200.00m, Description: "Monthly Rent Payment", Counterparty: "City Properties LLC", Category: "Housing", Essential: true),
            (Date: now.AddDays(-22), Amount: -1200.00m, Description: "Monthly Rent Payment", Counterparty: "City Properties LLC", Category: "Housing", Essential: true),
            
            // Utilities
            (Date: now.AddDays(-80), Amount: -85.50m, Description: "Electric Bill", Counterparty: "Power Company", Category: "Utilities", Essential: true),
            (Date: now.AddDays(-50), Amount: -92.30m, Description: "Electric Bill", Counterparty: "Power Company", Category: "Utilities", Essential: true),
            (Date: now.AddDays(-20), Amount: -88.75m, Description: "Electric Bill", Counterparty: "Power Company", Category: "Utilities", Essential: true),
            
            (Date: now.AddDays(-78), Amount: -45.00m, Description: "Internet Service", Counterparty: "FastNet ISP", Category: "Utilities", Essential: true),
            (Date: now.AddDays(-48), Amount: -45.00m, Description: "Internet Service", Counterparty: "FastNet ISP", Category: "Utilities", Essential: true),
            (Date: now.AddDays(-18), Amount: -45.00m, Description: "Internet Service", Counterparty: "FastNet ISP", Category: "Utilities", Essential: true),
            
            // Groceries
            (Date: now.AddDays(-83), Amount: -125.45m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-76), Amount: -98.20m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-69), Amount: -110.80m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-62), Amount: -135.60m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-53), Amount: -88.90m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-46), Amount: -115.30m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-39), Amount: -102.75m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-32), Amount: -94.50m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-23), Amount: -118.40m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-16), Amount: -107.20m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-9), Amount: -125.90m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            (Date: now.AddDays(-2), Amount: -96.30m, Description: "Supermarket", Counterparty: "Fresh Foods Market", Category: "Groceries", Essential: true),
            
            // Transportation
            (Date: now.AddDays(-81), Amount: -65.00m, Description: "Monthly Transit Pass", Counterparty: "City Transit", Category: "Transportation", Essential: true),
            (Date: now.AddDays(-51), Amount: -65.00m, Description: "Monthly Transit Pass", Counterparty: "City Transit", Category: "Transportation", Essential: true),
            (Date: now.AddDays(-21), Amount: -65.00m, Description: "Monthly Transit Pass", Counterparty: "City Transit", Category: "Transportation", Essential: true),
            
            (Date: now.AddDays(-70), Amount: -55.00m, Description: "Gas Station", Counterparty: "Shell", Category: "Transportation", Essential: false),
            (Date: now.AddDays(-40), Amount: -48.50m, Description: "Gas Station", Counterparty: "Shell", Category: "Transportation", Essential: false),
            (Date: now.AddDays(-10), Amount: -52.30m, Description: "Gas Station", Counterparty: "Shell", Category: "Transportation", Essential: false),
            
            // Dining Out
            (Date: now.AddDays(-84), Amount: -42.50m, Description: "Restaurant", Counterparty: "Italian Bistro", Category: "Dining", Essential: false),
            (Date: now.AddDays(-77), Amount: -28.90m, Description: "Coffee Shop", Counterparty: "Coffee Corner", Category: "Dining", Essential: false),
            (Date: now.AddDays(-71), Amount: -65.30m, Description: "Restaurant", Counterparty: "Sushi Place", Category: "Dining", Essential: false),
            (Date: now.AddDays(-64), Amount: -18.50m, Description: "Fast Food", Counterparty: "Burger Joint", Category: "Dining", Essential: false),
            (Date: now.AddDays(-57), Amount: -52.80m, Description: "Restaurant", Counterparty: "French Café", Category: "Dining", Essential: false),
            (Date: now.AddDays(-47), Amount: -35.20m, Description: "Restaurant", Counterparty: "Pizza House", Category: "Dining", Essential: false),
            (Date: now.AddDays(-37), Amount: -72.40m, Description: "Restaurant", Counterparty: "Steakhouse", Category: "Dining", Essential: false),
            (Date: now.AddDays(-27), Amount: -24.60m, Description: "Coffee Shop", Counterparty: "Coffee Corner", Category: "Dining", Essential: false),
            (Date: now.AddDays(-17), Amount: -48.90m, Description: "Restaurant", Counterparty: "Thai Express", Category: "Dining", Essential: false),
            (Date: now.AddDays(-7), Amount: -38.70m, Description: "Restaurant", Counterparty: "Mexican Grill", Category: "Dining", Essential: false),
            
            // Entertainment
            (Date: now.AddDays(-75), Amount: -15.99m, Description: "Netflix Subscription", Counterparty: "Netflix", Category: "Entertainment", Essential: false),
            (Date: now.AddDays(-45), Amount: -15.99m, Description: "Netflix Subscription", Counterparty: "Netflix", Category: "Entertainment", Essential: false),
            (Date: now.AddDays(-15), Amount: -15.99m, Description: "Netflix Subscription", Counterparty: "Netflix", Category: "Entertainment", Essential: false),
            
            (Date: now.AddDays(-74), Amount: -12.99m, Description: "Spotify Premium", Counterparty: "Spotify", Category: "Entertainment", Essential: false),
            (Date: now.AddDays(-44), Amount: -12.99m, Description: "Spotify Premium", Counterparty: "Spotify", Category: "Entertainment", Essential: false),
            (Date: now.AddDays(-14), Amount: -12.99m, Description: "Spotify Premium", Counterparty: "Spotify", Category: "Entertainment", Essential: false),
            
            (Date: now.AddDays(-68), Amount: -25.00m, Description: "Cinema Tickets", Counterparty: "Mega Cinema", Category: "Entertainment", Essential: false),
            (Date: now.AddDays(-35), Amount: -45.00m, Description: "Concert Tickets", Counterparty: "Live Music Hall", Category: "Entertainment", Essential: false),
            
            // Shopping
            (Date: now.AddDays(-63), Amount: -89.99m, Description: "Clothing Store", Counterparty: "Fashion Outlet", Category: "Shopping", Essential: false),
            (Date: now.AddDays(-42), Amount: -145.50m, Description: "Electronics", Counterparty: "Tech Store", Category: "Shopping", Essential: false),
            (Date: now.AddDays(-28), Amount: -65.00m, Description: "Books", Counterparty: "Bookstore", Category: "Shopping", Essential: false),
            (Date: now.AddDays(-12), Amount: -32.99m, Description: "Home Supplies", Counterparty: "HomeGoods", Category: "Shopping", Essential: false),
            
            // Healthcare
            (Date: now.AddDays(-60), Amount: -35.00m, Description: "Pharmacy", Counterparty: "City Pharmacy", Category: "Healthcare", Essential: true),
            (Date: now.AddDays(-30), Amount: -25.00m, Description: "Doctor Co-pay", Counterparty: "Medical Center", Category: "Healthcare", Essential: true),
            
            // Transfer to Savings
            (Date: now.AddDays(-54), Amount: -500.00m, Description: "Transfer to Savings", Counterparty: "Savings Account", Category: "Transfer", Essential: false),
            (Date: now.AddDays(-24), Amount: -500.00m, Description: "Transfer to Savings", Counterparty: "Savings Account", Category: "Transfer", Essential: false),
        };

        foreach (var (date, amount, description, counterparty, category, essential) in checkingTransactions)
        {
            transactions.Add(new MoneyMovement
            {
                Id = Guid.NewGuid(),
                FinancialAccountId = checking.Id,
                TransactionId = $"TXN_{Guid.NewGuid().ToString()[..8]}",
                TransactionDate = date,
                BookingDate = date,
                Amount = amount,
                CurrencyCode = "EUR",
                Description = description,
                CounterpartyName = counterparty,
                Category = category,
                IsEssentialExpense = essential,
                CreatedAt = date
            });
        }

        // Savings Account Transactions
        var savingsTransactions = new[]
        {
            // Transfers from checking
            (Date: now.AddDays(-54), Amount: 500.00m, Description: "Transfer from Checking", Counterparty: "Main Checking Account", Category: "Transfer", Essential: false),
            (Date: now.AddDays(-24), Amount: 500.00m, Description: "Transfer from Checking", Counterparty: "Main Checking Account", Category: "Transfer", Essential: false),
            
            // Interest
            (Date: now.AddDays(-60), Amount: 12.50m, Description: "Interest Payment", Counterparty: "Demo Bank", Category: "Income", Essential: false),
            (Date: now.AddDays(-30), Amount: 12.75m, Description: "Interest Payment", Counterparty: "Demo Bank", Category: "Income", Essential: false),
        };

        foreach (var (date, amount, description, counterparty, category, essential) in savingsTransactions)
        {
            transactions.Add(new MoneyMovement
            {
                Id = Guid.NewGuid(),
                FinancialAccountId = savings.Id,
                TransactionId = $"TXN_{Guid.NewGuid().ToString()[..8]}",
                TransactionDate = date,
                BookingDate = date,
                Amount = amount,
                CurrencyCode = "EUR",
                Description = description,
                CounterpartyName = counterparty,
                Category = category,
                IsEssentialExpense = essential,
                CreatedAt = date
            });
        }

        // Credit Card Transactions
        var creditCardTransactions = new[]
        {
            // Online Shopping
            (Date: now.AddDays(-80), Amount: -159.99m, Description: "Amazon Purchase", Counterparty: "Amazon", Category: "Shopping", Essential: false),
            (Date: now.AddDays(-65), Amount: -89.50m, Description: "Online Store", Counterparty: "eBay", Category: "Shopping", Essential: false),
            (Date: now.AddDays(-45), Amount: -245.00m, Description: "Laptop Accessories", Counterparty: "Tech Store Online", Category: "Electronics", Essential: false),
            (Date: now.AddDays(-25), Amount: -75.30m, Description: "Clothing", Counterparty: "Fashion Online", Category: "Shopping", Essential: false),
            
            // Subscriptions
            (Date: now.AddDays(-73), Amount: -9.99m, Description: "Cloud Storage", Counterparty: "Dropbox", Category: "Software", Essential: false),
            (Date: now.AddDays(-43), Amount: -9.99m, Description: "Cloud Storage", Counterparty: "Dropbox", Category: "Software", Essential: false),
            (Date: now.AddDays(-13), Amount: -9.99m, Description: "Cloud Storage", Counterparty: "Dropbox", Category: "Software", Essential: false),
            
            // Travel
            (Date: now.AddDays(-50), Amount: -350.00m, Description: "Hotel Booking", Counterparty: "Hotels.com", Category: "Travel", Essential: false),
            (Date: now.AddDays(-48), Amount: -120.00m, Description: "Flight Tickets", Counterparty: "Budget Airlines", Category: "Travel", Essential: false),
            
            // Payments
            (Date: now.AddDays(-26), Amount: 500.00m, Description: "Credit Card Payment", Counterparty: "Bank Transfer", Category: "Payment", Essential: false),
            (Date: now.AddDays(-5), Amount: 400.00m, Description: "Credit Card Payment", Counterparty: "Bank Transfer", Category: "Payment", Essential: false),
        };

        foreach (var (date, amount, description, counterparty, category, essential) in creditCardTransactions)
        {
            transactions.Add(new MoneyMovement
            {
                Id = Guid.NewGuid(),
                FinancialAccountId = creditCard.Id,
                TransactionId = $"TXN_{Guid.NewGuid().ToString()[..8]}",
                TransactionDate = date,
                BookingDate = date,
                Amount = amount,
                CurrencyCode = "EUR",
                Description = description,
                CounterpartyName = counterparty,
                Category = category,
                IsEssentialExpense = essential,
                CreatedAt = date
            });
        }

        // Investment Account Transactions
        var investmentTransactions = new[]
        {
            (Date: now.AddDays(-80), Amount: -2500.00m, Description: "Buy AAPL", Counterparty: "Trading212", Category: "Investment", Essential: false),
            (Date: now.AddDays(-75), Amount: -1500.00m, Description: "Buy MSFT", Counterparty: "Trading212", Category: "Investment", Essential: false),
            (Date: now.AddDays(-60), Amount: -1000.00m, Description: "Buy GOOGL", Counterparty: "Trading212", Category: "Investment", Essential: false),
            (Date: now.AddDays(-45), Amount: 500.00m, Description: "Dividend Payment", Counterparty: "Trading212", Category: "Income", Essential: false),
            (Date: now.AddDays(-30), Amount: -800.00m, Description: "Buy TSLA", Counterparty: "Trading212", Category: "Investment", Essential: false),
            (Date: now.AddDays(-15), Amount: 350.00m, Description: "Dividend Payment", Counterparty: "Trading212", Category: "Income", Essential: false),
            (Date: now.AddDays(-10), Amount: 1200.00m, Description: "Sell AAPL (profit)", Counterparty: "Trading212", Category: "Investment", Essential: false),
        };

        foreach (var (date, amount, description, counterparty, category, essential) in investmentTransactions)
        {
            transactions.Add(new MoneyMovement
            {
                Id = Guid.NewGuid(),
                FinancialAccountId = investment.Id,
                TransactionId = $"TXN_{Guid.NewGuid().ToString()[..8]}",
                ExternalId = $"T212_{Guid.NewGuid().ToString()[..12]}",
                TransactionDate = date,
                BookingDate = date,
                Amount = amount,
                CurrencyCode = "EUR",
                Description = description,
                CounterpartyName = counterparty,
                Category = category,
                IsEssentialExpense = essential,
                CreatedAt = date
            });
        }

        return transactions;
    }

    private List<CategorizationRule> CreateSampleCategorizationRules(string userId)
    {
        var now = DateTime.UtcNow;
        
        return new List<CategorizationRule>
        {
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Pattern = "Fresh Foods Market",
                Category = "Groceries",
                IsRegex = false,
                CaseSensitive = false,
                Priority = 10,
                MarkAsEssential = true,
                TransactionType = "Negative",
                CreatedAt = now.AddMonths(-3),
                TimesUsed = 12
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Pattern = "Salary",
                Category = "Income",
                IsRegex = false,
                CaseSensitive = false,
                Priority = 10,
                MarkAsEssential = false,
                TransactionType = "Positive",
                CreatedAt = now.AddMonths(-3),
                TimesUsed = 3
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Pattern = ".*Netflix.*|.*Spotify.*",
                Category = "Entertainment",
                IsRegex = true,
                CaseSensitive = false,
                Priority = 8,
                MarkAsEssential = false,
                TransactionType = "Negative",
                CreatedAt = now.AddMonths(-2),
                TimesUsed = 6
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Pattern = "Rent",
                Category = "Housing",
                IsRegex = false,
                CaseSensitive = false,
                Priority = 10,
                MarkAsEssential = true,
                TransactionType = "Negative",
                CreatedAt = now.AddMonths(-3),
                TimesUsed = 3
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Pattern = ".*Restaurant.*|.*Café.*|.*Coffee.*",
                Category = "Dining",
                IsRegex = true,
                CaseSensitive = false,
                Priority = 7,
                MarkAsEssential = false,
                TransactionType = "Negative",
                CreatedAt = now.AddMonths(-2),
                TimesUsed = 10
            }
        };
    }

    private void UpdateAccountBalances(List<FinancialAccount> accounts, List<MoneyMovement> transactions)
    {
        foreach (var account in accounts)
        {
            var accountTransactions = transactions.Where(t => t.FinancialAccountId == account.Id);
            var totalMovements = accountTransactions.Sum(t => t.Amount);
            account.CurrentBalance = account.StartingBalance + totalMovements;
        }
    }
}
