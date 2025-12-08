using Bankweave.Entities;
using Bankweave.Infrastructure;
using Bankweave.Services;
using Bankweave.Services.CsvParsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CsvImportController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CsvImportController> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TransactionCategorizationService _categorizationService;

    public CsvImportController(
        AppDbContext dbContext, 
        ILogger<CsvImportController> logger, 
        ILoggerFactory loggerFactory,
        TransactionCategorizationService categorizationService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _categorizationService = categorizationService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCsv([FromForm] CsvUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        if (!request.File.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "File must be a CSV" });
        }

        try
        {
            using var reader = new StreamReader(request.File.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();

            _logger.LogInformation("Processing CSV import for provider {Provider}, account {AccountName}", 
                request.Provider, request.AccountName);

            var parser = GetParser(request.Provider);
            var parseResult = parser.Parse(csvContent, request.AccountName);

            _logger.LogInformation("Parser returned {TransactionCount} transactions", parseResult.Transactions.Count);
            
            if (parseResult.DetectedBalance.HasValue)
            {
                _logger.LogInformation("Detected balance from CSV: {Balance:C}", parseResult.DetectedBalance.Value);
            }

            // Get or create account
            var account = _dbContext.FinancialAccounts
                .FirstOrDefault(a => a.Provider == request.Provider && a.DisplayName == request.AccountName);

            bool isNewAccount = false;
            if (account == null)
            {
                isNewAccount = true;
                account = new FinancialAccount
                {
                    Id = Guid.NewGuid(),
                    Provider = request.Provider,
                    DisplayName = request.AccountName,
                    ExternalId = $"csv-{request.Provider}-{Guid.NewGuid()}",
                    CreatedAt = DateTime.UtcNow,
                    CurrencyCode = "EUR",
                    StartingBalance = 0 // Will be set by frontend confirmation
                };
                _dbContext.FinancialAccounts.Add(account);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Created new account {AccountId} for {Provider}", account.Id, request.Provider);
            }

            // Import transactions
            int importedCount = 0;
            int skippedCount = 0;

            foreach (var txn in parseResult.Transactions)
            {
                // Check if transaction already exists by TransactionId OR by unique combination
                // Also check CounterpartyName to handle cases where it's set instead of description
                var exists = _dbContext.MoneyMovements
                    .Any(m => m.FinancialAccountId == account.Id && 
                             ((!string.IsNullOrEmpty(txn.TransactionId) && m.TransactionId == txn.TransactionId) ||
                              (m.TransactionDate == txn.TransactionDate &&
                               m.Amount == txn.Amount &&
                               (m.Description == txn.Description || 
                                (!string.IsNullOrEmpty(txn.CounterpartyName) && m.CounterpartyName == txn.CounterpartyName)))));

                if (exists)
                {
                    skippedCount++;
                    continue;
                }

                txn.FinancialAccountId = account.Id;
                txn.Id = Guid.NewGuid();
                txn.CreatedAt = DateTime.UtcNow;
                
                // Auto-categorize if no category set - use async method to check learned categories
                if (string.IsNullOrEmpty(txn.Category))
                {
                    txn.Category = await _categorizationService.CategorizeTransactionAsync(
                        txn.Description, 
                        txn.CounterpartyName, 
                        txn.Amount);
                }

                _dbContext.MoneyMovements.Add(txn);
                importedCount++;
            }

            // Save transactions first
            await _dbContext.SaveChangesAsync();

            // For credit cards, don't update the balance from CSV imports
            // User will manually update the balance, and transactions are just for analytics
            if (!account.IsCreditCard)
            {
                // If balance was detected from CSV (like PTSB), use it directly as current balance
                // PTSB CSVs show the balance AFTER each transaction, so the first balance is the current balance
                if (parseResult.DetectedBalance.HasValue && parseResult.HasBalanceColumn)
                {
                    account.CurrentBalance = parseResult.DetectedBalance.Value;
                    account.StartingBalance = 0; // Not applicable for CSVs with running balance
                }
                // If no balance detected, we'll set it to 0 and let user update it manually
                else
                {
                    account.CurrentBalance = 0;
                    account.StartingBalance = 0;
                }
            }
            
            account.LastSyncedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("CSV import complete: {ImportedCount} imported, {SkippedCount} skipped, Balance: {Balance:C}", 
                importedCount, skippedCount, account.CurrentBalance);

            return Ok(new
            {
                message = "CSV imported successfully",
                accountId = account.Id,
                importedCount,
                skippedCount,
                detectedBalance = parseResult.DetectedBalance,
                hasBalanceColumn = parseResult.HasBalanceColumn,
                currentBalance = account.CurrentBalance,
                isNewAccount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV");
            return StatusCode(500, new { error = $"Failed to import CSV: {ex.Message}" });
        }
    }

    [HttpPost("set-balance/{accountId}")]
    public async Task<IActionResult> SetAccountBalance(Guid accountId, [FromBody] SetBalanceRequest request)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        
        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        // When user manually sets balance, use it as the current balance directly
        // This is typically done when CSV doesn't have balance column (like Trading212)
        account.CurrentBalance = request.StartingBalance;
        account.StartingBalance = request.StartingBalance;
        
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated account {AccountId} starting balance to {Balance:C}, current balance: {CurrentBalance:C}", 
            accountId, account.StartingBalance, account.CurrentBalance);

        return Ok(new
        {
            message = "Balance updated successfully",
            startingBalance = account.StartingBalance,
            currentBalance = account.CurrentBalance
        });
    }

    [HttpPost("manual-account")]
    public async Task<IActionResult> CreateManualAccount([FromBody] ManualAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountName))
        {
            return BadRequest(new { error = "Account name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return BadRequest(new { error = "Provider is required" });
        }

        // Check if account already exists
        var existingAccount = _dbContext.FinancialAccounts
            .FirstOrDefault(a => a.Provider == request.Provider && a.DisplayName == request.AccountName);

        if (existingAccount != null)
        {
            return BadRequest(new { error = "An account with this name and provider already exists" });
        }

        // Create new account
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            Provider = request.Provider,
            DisplayName = request.AccountName,
            CurrentBalance = request.CurrentBalance,
            StartingBalance = request.CurrentBalance,
            IsCreditCard = request.IsCreditCard,
            CurrencyCode = "EUR",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };

        _dbContext.FinancialAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created manual account {AccountId} for provider {Provider} with balance {Balance:C}, IsCreditCard: {IsCreditCard}",
            account.Id, request.Provider, request.CurrentBalance, request.IsCreditCard);

        return Ok(new
        {
            message = "Account created successfully",
            accountId = account.Id,
            displayName = account.DisplayName,
            currentBalance = account.CurrentBalance
        });
    }

    private ICsvParser GetParser(string provider)
    {
        _logger.LogInformation("Creating parser for provider: {Provider}", provider);
        
        return provider.ToLowerInvariant() switch
        {
            "trading212" => new Trading212CsvParser(_loggerFactory.CreateLogger<Trading212CsvParser>()),
            "traderepublic" => new TradeRepublicCsvParser(_loggerFactory.CreateLogger<TradeRepublicCsvParser>()),
            "raisin" => new RaisinCsvParser(_loggerFactory.CreateLogger<RaisinCsvParser>()),
            "revolut" => new RevolutCsvParser(_loggerFactory.CreateLogger<RevolutCsvParser>()),
            "ptsb" => new PtsbCsvParser(_loggerFactory.CreateLogger<PtsbCsvParser>()),
            _ => new GenericCsvParser(_loggerFactory.CreateLogger<GenericCsvParser>())
        };
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncBalanceFromCsv([FromForm] CsvSyncRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (!request.AccountId.HasValue)
        {
            return BadRequest(new { error = "Account ID is required" });
        }

        var account = await _dbContext.FinancialAccounts.FindAsync(request.AccountId.Value);
        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        try
        {
            string csvContent;
            using (var reader = new StreamReader(request.File.OpenReadStream()))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            var parser = GetParser(request.Provider);
            var parseResult = parser.Parse(csvContent, account.DisplayName);

            if (parseResult.Transactions.Count == 0)
            {
                return BadRequest(new { error = "No transactions found in CSV" });
            }

            var transactionsImported = 0;
            var skippedDuplicates = 0;
            
            foreach (var transaction in parseResult.Transactions)
            {
                // Check for duplicates using the same robust logic as the upload endpoint
                var exists = await _dbContext.MoneyMovements
                    .AnyAsync(m => m.FinancialAccountId == account.Id && 
                             ((!string.IsNullOrEmpty(transaction.TransactionId) && m.TransactionId == transaction.TransactionId) ||
                              (m.TransactionDate == transaction.TransactionDate &&
                               m.Amount == transaction.Amount &&
                               (m.Description == transaction.Description || 
                                (!string.IsNullOrEmpty(transaction.CounterpartyName) && m.CounterpartyName == transaction.CounterpartyName)))));

                if (exists)
                {
                    skippedDuplicates++;
                    continue;
                }
                
                transaction.FinancialAccountId = account.Id;
                
                // Auto-categorize if no category set - use async method to check learned categories
                if (string.IsNullOrEmpty(transaction.Category))
                {
                    transaction.Category = await _categorizationService.CategorizeTransactionAsync(
                        transaction.Description, 
                        transaction.CounterpartyName, 
                        transaction.Amount);
                }
                
                _dbContext.MoneyMovements.Add(transaction);
                transactionsImported++;
            }

            // Update balance if detected and account is not a credit card
            if (!account.IsCreditCard && parseResult.DetectedBalance.HasValue && parseResult.HasBalanceColumn)
            {
                account.CurrentBalance = parseResult.DetectedBalance.Value;
            }

            account.LastSyncedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Synced account {AccountId} from CSV. Balance: {Balance}, Imported: {Imported}, Skipped: {Skipped}",
                account.Id, account.CurrentBalance, transactionsImported, skippedDuplicates);

            return Ok(new
            {
                message = "Balance synced successfully",
                newBalance = account.CurrentBalance,
                transactionsImported,
                skippedDuplicates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing CSV for account {AccountId}", request.AccountId);
            return StatusCode(500, new { error = "Failed to process CSV file" });
        }
    }
    
    [HttpPost("remove-duplicates")]
    public async Task<IActionResult> RemoveDuplicates()
    {
        try
        {
            // Find duplicates: same account, date, amount, and description
            var allTransactions = await _dbContext.MoneyMovements
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var duplicateGroups = allTransactions
                .GroupBy(m => new { 
                    m.FinancialAccountId, 
                    m.TransactionDate, 
                    m.Amount, 
                    m.Description,
                    m.CounterpartyName
                })
                .Where(g => g.Count() > 1)
                .ToList();

            int removedCount = 0;

            foreach (var group in duplicateGroups)
            {
                // Keep the first transaction (oldest CreatedAt), remove the rest
                var toRemove = group.OrderBy(m => m.CreatedAt).Skip(1).ToList();
                _dbContext.MoneyMovements.RemoveRange(toRemove);
                removedCount += toRemove.Count;
                
                _logger.LogInformation("Removing {Count} duplicates for transaction on {Date}: {Description}", 
                    toRemove.Count, group.Key.TransactionDate, group.Key.Description);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Removed {Count} duplicate transactions across {GroupCount} groups", 
                removedCount, duplicateGroups.Count);

            return Ok(new
            {
                message = $"Removed {removedCount} duplicate transactions",
                duplicateGroupsFound = duplicateGroups.Count,
                transactionsRemoved = removedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove duplicates");
            return StatusCode(500, new { error = $"Failed to remove duplicates: {ex.Message}" });
        }
    }
}

public class CsvUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Provider { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

public class SetBalanceRequest
{
    public decimal StartingBalance { get; set; }
}

public class ManualAccountRequest
{
    public string Provider { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsCreditCard { get; set; } = false;
}

public class CsvSyncRequest
{
    public IFormFile File { get; set; } = null!;
    public string Provider { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
}
