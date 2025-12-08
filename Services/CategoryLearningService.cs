using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bankweave.Services;

public class CategoryLearningService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CategoryLearningService> _logger;

    public CategoryLearningService(AppDbContext dbContext, ILogger<CategoryLearningService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> LearnFromManualCategorizationAsync(Guid transactionId, string newCategory)
    {
        var transaction = await _dbContext.MoneyMovements
            .FirstOrDefaultAsync(m => m.Id == transactionId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found for learning", transactionId);
            return 0;
        }

        // Extract the key identifier (counterparty or description pattern)
        var identifier = GetTransactionIdentifier(transaction);
        
        if (string.IsNullOrWhiteSpace(identifier))
        {
            _logger.LogInformation("No clear identifier found for transaction {TransactionId}", transactionId);
            return 0;
        }

        // Find all similar transactions and update their categories
        var similarTransactions = await _dbContext.MoneyMovements
            .Where(m => m.CounterpartyName != null && m.CounterpartyName.Contains(identifier) ||
                       m.Description != null && m.Description.Contains(identifier))
            .ToListAsync();

        _logger.LogInformation("Found {Count} similar transactions for identifier '{Identifier}'", 
            similarTransactions.Count, identifier);

        foreach (var tx in similarTransactions)
        {
            tx.Category = newCategory;
        }

        await _dbContext.SaveChangesAsync();
        
        return similarTransactions.Count;
    }

    private string? GetTransactionIdentifier(MoneyMovement transaction)
    {
        // Prefer counterparty name as it's usually more specific
        if (!string.IsNullOrWhiteSpace(transaction.CounterpartyName))
        {
            // Clean up common prefixes
            var counterparty = transaction.CounterpartyName;
            
            // Remove common payment method prefixes
            var prefixes = new[] { "Card debit - ", "Card payment - ", "Direct Debit - ", "Standing Order - " };
            foreach (var prefix in prefixes)
            {
                if (counterparty.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    counterparty = counterparty.Substring(prefix.Length);
                    break;
                }
            }

            return counterparty.Trim();
        }

        // Fall back to description if counterparty is not available
        if (!string.IsNullOrWhiteSpace(transaction.Description))
        {
            return transaction.Description.Trim();
        }

        return null;
    }

    public async Task<string?> GetLearnedCategoryAsync(string counterparty, string description)
    {
        // Try to find an existing transaction with the same pattern that has been categorized
        var identifier = ExtractIdentifier(counterparty, description);
        
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var existingTransaction = await _dbContext.MoneyMovements
            .Where(m => m.Category != null && m.Category != "Other" &&
                       (m.CounterpartyName != null && m.CounterpartyName.Contains(identifier) ||
                        m.Description != null && m.Description.Contains(identifier)))
            .Select(m => m.Category)
            .FirstOrDefaultAsync();

        return existingTransaction;
    }

    private string? ExtractIdentifier(string? counterparty, string? description)
    {
        if (!string.IsNullOrWhiteSpace(counterparty))
        {
            var cleaned = counterparty;
            var prefixes = new[] { "Card debit - ", "Card payment - ", "Direct Debit - ", "Standing Order - " };
            foreach (var prefix in prefixes)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length);
                    break;
                }
            }
            return cleaned.Trim();
        }

        return description?.Trim();
    }
}
