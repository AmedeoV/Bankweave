using Bankweave.Infrastructure;
using Bankweave.Entities;
using Bankweave.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StatsController> _logger;
    private readonly RecurringTransactionAnalyzer _recurringAnalyzer;

    public StatsController(
        AppDbContext dbContext, 
        ILogger<StatsController> logger,
        RecurringTransactionAnalyzer recurringAnalyzer)
    {
        _dbContext = dbContext;
        _logger = logger;
        _recurringAnalyzer = recurringAnalyzer;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accounts = await _dbContext.FinancialAccounts.Where(a => a.UserId == userId).ToListAsync();
        var totalBalance = accounts.Where(a => !a.ExcludeFromTotal).Sum(a => a.CurrentBalance);

        // Determine date range
        DateTime queryStartDate;
        DateTime? queryEndDate = null;
        
        if (startDate.HasValue)
        {
            queryStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        }
        else
        {
            queryStartDate = DateTime.UtcNow.AddDays(-30);
        }
        
        if (endDate.HasValue)
        {
            queryEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1);
        }

        var query = _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= queryStartDate)
            .Where(m => m.Category != "Transfer" && m.Category != "Transfer In" && m.Category != "Transfers" && m.Category != "Balance Payment");
            
        if (queryEndDate.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= queryEndDate.Value);
        }
        
        var recentTransactions = await query.ToListAsync();

        var income = recentTransactions.Where(m => m.Amount > 0).Sum(m => m.Amount);
        var expenses = recentTransactions.Where(m => m.Amount < 0).Sum(m => Math.Abs(m.Amount));
        var essentialExpenses = recentTransactions.Where(m => m.Amount < 0 && m.IsEssentialExpense).Sum(m => Math.Abs(m.Amount));

        return Ok(new
        {
            totalBalance,
            accountCount = accounts.Count,
            income30Days = income,
            expenses30Days = expenses,
            essentialExpenses30Days = essentialExpenses,
            net30Days = income - expenses
        });
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyStats([FromQuery] int months = 6)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var transactions = await _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= startDate)
            .Where(m => m.Category != "Transfer" && m.Category != "Transfer In" && m.Category != "Transfers" && m.Category != "Balance Payment")
            .ToListAsync();

        var monthlyStats = transactions
            .GroupBy(m => new { m.TransactionDate.Year, m.TransactionDate.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                income = g.Where(m => m.Amount > 0).Sum(m => m.Amount),
                expenses = g.Where(m => m.Amount < 0).Sum(m => Math.Abs(m.Amount)),
                net = g.Sum(m => m.Amount),
                transactionCount = g.Count()
            })
            .OrderBy(s => s.year)
            .ThenBy(s => s.month)
            .ToList();

        return Ok(monthlyStats);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategoryBreakdown(
        [FromQuery] int? days = null, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        DateTime queryStartDate;
        DateTime? queryEndDate = null;
        
        if (startDate.HasValue && endDate.HasValue)
        {
            queryStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            queryEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1);
        }
        else if (days.HasValue)
        {
            queryStartDate = DateTime.UtcNow.AddDays(-days.Value);
        }
        else
        {
            queryStartDate = DateTime.UtcNow.AddDays(-30);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= queryStartDate && m.Amount < 0);
            
        if (queryEndDate.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= queryEndDate.Value);
        }
        
        var transactions = await query.ToListAsync();

        var categoryStats = transactions
            .GroupBy(m => m.Category ?? "Uncategorized")
            .Select(g => new
            {
                category = g.Key,
                total = Math.Abs(g.Sum(m => m.Amount)),
                count = g.Count()
            })
            .OrderByDescending(s => s.total)
            .ToList();

        return Ok(categoryStats);
    }

    [HttpGet("income-categories")]
    public async Task<IActionResult> GetIncomeCategoryBreakdown(
        [FromQuery] int? days = null, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        DateTime queryStartDate;
        DateTime? queryEndDate = null;
        
        if (startDate.HasValue && endDate.HasValue)
        {
            queryStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            queryEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1);
        }
        else if (days.HasValue)
        {
            queryStartDate = DateTime.UtcNow.AddDays(-days.Value);
        }
        else
        {
            queryStartDate = DateTime.UtcNow.AddDays(-30);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= queryStartDate && m.Amount > 0)
            .Where(m => m.Category != "Transfer" && m.Category != "Transfer In" && m.Category != "Transfers" && m.Category != "Balance Payment");
            
        if (queryEndDate.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= queryEndDate.Value);
        }
        
        var transactions = await query.ToListAsync();

        var categoryStats = transactions
            .GroupBy(m => m.Category ?? "Uncategorized")
            .Select(g => new
            {
                category = g.Key,
                total = g.Sum(m => m.Amount),
                count = g.Count()
            })
            .OrderByDescending(s => s.total)
            .ToList();

        return Ok(categoryStats);
    }

    [HttpGet("top-merchants")]
    public async Task<IActionResult> GetTopMerchants([FromQuery] int days = 30, [FromQuery] int limit = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var startDate = DateTime.UtcNow.AddDays(-days);
        var transactions = await _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= startDate && m.Amount < 0 && m.CounterpartyName != null)
            .ToListAsync();

        var topMerchants = transactions
            .GroupBy(m => m.CounterpartyName!)
            .Select(g => new
            {
                merchant = g.Key,
                total = Math.Abs(g.Sum(m => m.Amount)),
                count = g.Count(),
                averageAmount = Math.Abs(g.Average(m => m.Amount))
            })
            .OrderByDescending(s => s.total)
            .Take(limit)
            .ToList();

        return Ok(topMerchants);
    }

    [HttpPost("snapshot")]
    public async Task<IActionResult> CreateBalanceSnapshot()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accounts = await _dbContext.FinancialAccounts.Where(a => a.UserId == userId).ToListAsync();
        var totalBalance = accounts.Where(a => !a.ExcludeFromTotal).Sum(a => a.CurrentBalance);

        // Store breakdown of each account for detailed analysis
        var accountBalances = accounts.Select(a => new
        {
            id = a.Id,
            name = a.DisplayName,
            balance = a.CurrentBalance,
            excluded = a.ExcludeFromTotal
        }).ToList();

        var snapshot = new BalanceSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            TotalBalance = totalBalance,
            AccountBalances = JsonSerializer.Serialize(accountBalances)
        };

        _dbContext.BalanceSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Balance snapshot created: €{totalBalance:F2} at {snapshot.Timestamp}");

        return Ok(new { 
            id = snapshot.Id, 
            timestamp = snapshot.Timestamp, 
            totalBalance = snapshot.TotalBalance 
        });
    }

    [HttpGet("recurring-transactions")]
    public async Task<IActionResult> GetRecurringTransactions(
        [FromQuery] int? months = null,
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        DateTime? utcStartDate = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : null;
        DateTime? utcEndDate = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1) : null;
        
        var result = await _recurringAnalyzer.AnalyzeRecurringTransactionsAsync(userId, months, utcStartDate, utcEndDate);
        
        return Ok(new
        {
            expenses = result.Expenses.Select(p => new
            {
                name = p.Name,
                category = p.Category,
                averageAmount = p.AverageAmount,
                minAmount = p.MinAmount,
                maxAmount = p.MaxAmount,
                frequency = p.Frequency,
                occurrences = p.Occurrences,
                lastDate = p.LastDate,
                averageDaysBetween = p.AverageDaysBetween,
                nextExpectedDate = p.NextExpectedDate,
                isEssentialExpense = p.IsEssentialExpense
            }),
            income = result.Income.Select(p => new
            {
                name = p.Name,
                category = p.Category,
                averageAmount = p.AverageAmount,
                minAmount = p.MinAmount,
                maxAmount = p.MaxAmount,
                frequency = p.Frequency,
                occurrences = p.Occurrences,
                lastDate = p.LastDate,
                averageDaysBetween = p.AverageDaysBetween,
                nextExpectedDate = p.NextExpectedDate
            })
        });
    }

    [HttpGet("balance-history")]
    public async Task<IActionResult> GetBalanceHistory(
        [FromQuery] int? months = null,
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        DateTime queryStartDate;
        DateTime? queryEndDate = null;
        
        if (startDate.HasValue)
        {
            queryStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        }
        else if (months.HasValue)
        {
            queryStartDate = DateTime.UtcNow.AddMonths(-months.Value);
        }
        else
        {
            queryStartDate = DateTime.UtcNow.AddMonths(-12);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _dbContext.BalanceSnapshots
            .Where(s => s.UserId == userId)
            .Where(s => s.Timestamp >= queryStartDate);
            
        if (endDate.HasValue)
        {
            queryEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1);
            query = query.Where(s => s.Timestamp <= queryEndDate.Value);
        }
        
        var snapshots = await query
            .OrderBy(s => s.Timestamp)
            .ToListAsync();

        return Ok(snapshots.Select(s => new
        {
            timestamp = s.Timestamp,
            totalBalance = s.TotalBalance,
            accountBalances = s.AccountBalances != null 
                ? JsonSerializer.Deserialize<object>(s.AccountBalances) 
                : null
        }));
    }

    [HttpGet("essential-expenses")]
    public async Task<IActionResult> GetEssentialExpenses(
        [FromQuery] int? days = null,
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        DateTime queryStartDate;
        DateTime? queryEndDate = null;
        
        if (startDate.HasValue && endDate.HasValue)
        {
            queryStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            queryEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc).AddDays(1).AddSeconds(-1);
        }
        else if (days.HasValue)
        {
            queryStartDate = DateTime.UtcNow.AddDays(-days.Value);
        }
        else
        {
            queryStartDate = DateTime.UtcNow.AddDays(-30);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= queryStartDate && m.Amount < 0 && m.IsEssentialExpense);
            
        if (queryEndDate.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= queryEndDate.Value);
        }
        
        var transactions = await query.OrderByDescending(m => m.TransactionDate).ToListAsync();

        var total = transactions.Sum(m => Math.Abs(m.Amount));
        
        var byCategory = transactions
            .GroupBy(m => m.Category ?? "Uncategorized")
            .Select(g => new
            {
                category = g.Key,
                total = Math.Abs(g.Sum(m => m.Amount)),
                count = g.Count()
            })
            .OrderByDescending(s => s.total)
            .ToList();

        return Ok(new
        {
            total,
            count = transactions.Count,
            byCategory,
            transactions = transactions.Select(t => new
            {
                id = t.Id,
                date = t.TransactionDate,
                amount = t.Amount,
                description = t.Description,
                counterpartyName = t.CounterpartyName,
                category = t.Category
            })
        });
    }
}
