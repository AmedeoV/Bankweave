using Bankweave.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bankweave.Services;

public class RecurringTransactionAnalyzer
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RecurringTransactionAnalyzer> _logger;

    public RecurringTransactionAnalyzer(AppDbContext dbContext, ILogger<RecurringTransactionAnalyzer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RecurringTransactionsResult> AnalyzeRecurringTransactionsAsync(
        string userId,
        int? monthsToAnalyze = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        DateTime cutoffDate;
        
        if (startDate.HasValue)
        {
            cutoffDate = startDate.Value;
        }
        else if (monthsToAnalyze.HasValue)
        {
            cutoffDate = DateTime.UtcNow.AddMonths(-monthsToAnalyze.Value);
        }
        else
        {
            cutoffDate = DateTime.UtcNow.AddMonths(-6);
        }
        
        var query = _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .Where(m => m.TransactionDate >= cutoffDate)
            .Where(m => m.Category != "Transfer" && m.Category != "Transfer In" && m.Category != "Transfers" && m.Category != "Balance Payment");
            
        if (endDate.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= endDate.Value);
        }
        
        var allTransactions = await query
            .OrderBy(m => m.TransactionDate)
            .ToListAsync();

        var expenses = allTransactions.Where(t => t.Amount < 0).ToList();
        var income = allTransactions.Where(t => t.Amount > 0).ToList();

        var recurringExpenses = AnalyzeTransactionGroup(expenses);
        var recurringIncome = AnalyzeTransactionGroup(income);

        return new RecurringTransactionsResult
        {
            Expenses = recurringExpenses,
            Income = recurringIncome
        };
    }

    private List<RecurringTransactionPattern> AnalyzeTransactionGroup(List<Entities.MoneyMovement> transactions)
    {
        // Group by counterparty and description patterns
        var groupedTransactions = transactions
            .GroupBy(t => new { 
                Counterparty = t.CounterpartyName ?? t.Description,
                AmountRange = Math.Round(t.Amount / 10) * 10 // Group similar amounts (within 10 euros)
            })
            .Where(g => g.Count() >= 2) // At least 2 occurrences
            .ToList();

        var recurringPatterns = new List<RecurringTransactionPattern>();

        foreach (var group in groupedTransactions)
        {
            var groupTransactions = group.OrderBy(t => t.TransactionDate).ToList();
            
            // Calculate intervals between transactions
            var intervals = new List<int>();
            for (int i = 1; i < groupTransactions.Count; i++)
            {
                var daysBetween = (groupTransactions[i].TransactionDate - groupTransactions[i-1].TransactionDate).Days;
                intervals.Add(daysBetween);
            }

            // Check if intervals are consistent (monthly = 28-35 days)
            var avgInterval = intervals.Average();
            var isMonthly = avgInterval >= 25 && avgInterval <= 35;
            
            if (isMonthly)
            {
                var pattern = new RecurringTransactionPattern
                {
                    Name = group.Key.Counterparty ?? "Unknown",
                    Category = groupTransactions.First().Category ?? "Other",
                    AverageAmount = groupTransactions.Average(t => t.Amount),
                    MinAmount = groupTransactions.Min(t => t.Amount),
                    MaxAmount = groupTransactions.Max(t => t.Amount),
                    Frequency = "Monthly",
                    Occurrences = groupTransactions.Count,
                    LastDate = groupTransactions.Last().TransactionDate,
                    AverageDaysBetween = (int)avgInterval,
                    NextExpectedDate = groupTransactions.Last().TransactionDate.AddDays((int)avgInterval),
                    IsEssentialExpense = groupTransactions.Any(t => t.IsEssentialExpense)
                };

                recurringPatterns.Add(pattern);
            }
        }

        return recurringPatterns
            .OrderByDescending(p => Math.Abs(p.AverageAmount))
            .ToList();
    }
}

public class RecurringTransactionPattern
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal AverageAmount { get; set; }
    public bool IsEssentialExpense { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public DateTime LastDate { get; set; }
    public int AverageDaysBetween { get; set; }
    public DateTime NextExpectedDate { get; set; }
}

public class RecurringTransactionsResult
{
    public List<RecurringTransactionPattern> Expenses { get; set; } = new();
    public List<RecurringTransactionPattern> Income { get; set; } = new();
}
