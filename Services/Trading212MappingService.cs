using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bankweave.Services;

public class Trading212MappingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<Trading212MappingService> _logger;

    public Trading212MappingService(AppDbContext context, ILogger<Trading212MappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to find a matching CSV-imported transaction for an API transaction
    /// Matches based on date, amount, and type similarity
    /// </summary>
    public async Task<MoneyMovement?> FindMatchingCsvTransaction(
        Guid accountId,
        DateTime apiDateTime,
        decimal apiAmount,
        string apiType)
    {
        // Look for transactions within 24 hours of the API transaction
        var startDate = apiDateTime.AddHours(-24);
        var endDate = apiDateTime.AddHours(24);

        var potentialMatches = await _context.MoneyMovements
            .Where(m => m.FinancialAccountId == accountId
                && m.ExternalId != null  // Must have a CSV ID
                && m.TransactionDate >= startDate
                && m.TransactionDate <= endDate
                && m.Amount == apiAmount)
            .ToListAsync();

        if (!potentialMatches.Any())
        {
            _logger.LogDebug("No matching CSV transactions found for API transaction: {Type} {Amount} on {Date}",
                apiType, apiAmount, apiDateTime);
            return null;
        }

        // If multiple matches, prefer exact time match
        var exactMatch = potentialMatches
            .FirstOrDefault(m => Math.Abs((m.TransactionDate - apiDateTime).TotalMinutes) < 1);

        if (exactMatch != null)
        {
            _logger.LogInformation("Found exact CSV match for API transaction: ExternalId={ExternalId}",
                exactMatch.ExternalId);
            return exactMatch;
        }

        // Otherwise return the closest time match
        var closestMatch = potentialMatches
            .OrderBy(m => Math.Abs((m.TransactionDate - apiDateTime).Ticks))
            .First();

        _logger.LogInformation("Found close CSV match for API transaction: ExternalId={ExternalId}, TimeDiff={TimeDiff}min",
            closestMatch.ExternalId, (closestMatch.TransactionDate - apiDateTime).TotalMinutes);

        return closestMatch;
    }

    /// <summary>
    /// Updates an existing CSV transaction with API transaction ID
    /// This links the two representations of the same transaction
    /// </summary>
    public async Task<bool> LinkApiToExistingTransaction(MoneyMovement existingTransaction, string apiTransactionUuid)
    {
        try
        {
            // Update TransactionId to use the UUID from API
            existingTransaction.TransactionId = $"t212-api-{apiTransactionUuid}";
            
            _logger.LogInformation("Linked API transaction {ApiId} to CSV transaction with ExternalId={ExternalId}",
                apiTransactionUuid, existingTransaction.ExternalId);

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link API transaction {ApiId} to CSV transaction", apiTransactionUuid);
            return false;
        }
    }

    /// <summary>
    /// Creates a new MoneyMovement from an API transaction
    /// </summary>
    public MoneyMovement CreateFromApiTransaction(
        Guid accountId,
        Trading212Transaction apiTransaction)
    {
        // Use Reference (UUID) as the unique identifier, fallback to Id if Reference is empty
        var uniqueId = !string.IsNullOrEmpty(apiTransaction.Reference) 
            ? apiTransaction.Reference 
            : apiTransaction.Id.ToString();

        return new MoneyMovement
        {
            FinancialAccountId = accountId,
            TransactionId = $"t212-api-{uniqueId}",
            ExternalId = uniqueId,  // Store UUID for matching with CSV transactions
            TransactionDate = apiTransaction.DateTime,
            BookingDate = apiTransaction.DateTime,
            Amount = apiTransaction.Amount,
            Description = $"{apiTransaction.Type} - {apiTransaction.Reference}",
            CurrencyCode = "EUR",  // Trading212 API uses EUR for cash account
            Category = DetermineCategory(apiTransaction.Type)
        };
    }

    private string DetermineCategory(string type)
    {
        return type.ToLower() switch
        {
            var t when t.Contains("interest") => "Interest",
            var t when t.Contains("card") => "Expense",
            var t when t.Contains("deposit") => "Income",
            var t when t.Contains("withdrawal") => "Expense",
            var t when t.Contains("cashback") => "Income",
            _ => "Other"
        };
    }

    /// <summary>
    /// Gets all CSV transactions with external IDs for an account
    /// Useful for reconciliation and reporting
    /// </summary>
    public async Task<List<MoneyMovement>> GetCsvTransactionsWithExternalIds(Guid accountId)
    {
        return await _context.MoneyMovements
            .Where(m => m.FinancialAccountId == accountId && m.ExternalId != null)
            .OrderBy(m => m.TransactionDate)
            .ToListAsync();
    }
}
