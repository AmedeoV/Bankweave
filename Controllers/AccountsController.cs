using Bankweave.Infrastructure;
using Bankweave.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AccountsController> _logger;
    private readonly Trading212ApiService _trading212Service;
    private readonly TransactionCategorizationService _categorizationService;
    private readonly Trading212MappingService _trading212MappingService;
    private readonly CategoryLearningService _categoryLearningService;

    public AccountsController(
        AppDbContext dbContext, 
        ILogger<AccountsController> logger, 
        Trading212ApiService trading212Service,
        TransactionCategorizationService categorizationService,
        Trading212MappingService trading212MappingService,
        CategoryLearningService categoryLearningService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _trading212Service = trading212Service;
        _categorizationService = categorizationService;
        _trading212MappingService = trading212MappingService;
        _categoryLearningService = categoryLearningService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var accounts = await _dbContext.FinancialAccounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Provider)
            .ThenBy(a => a.DisplayName)
            .Select(a => new
            {
                id = a.Id,
                displayName = a.DisplayName,
                provider = a.Provider,
                iban = a.Iban,
                balance = a.CurrentBalance,
                currency = a.CurrencyCode,
                lastSynced = a.LastSyncedAt,
                excludeFromTotal = a.ExcludeFromTotal,
                isCreditCard = a.IsCreditCard
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var query = _dbContext.MoneyMovements
            .Include(m => m.Account)
            .Where(m => m.Account!.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(m => m.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= to.Value);
        }

        var transactions = await query
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new
            {
                id = m.Id,
                transactionId = m.TransactionId,
                date = m.TransactionDate,
                bookingDate = m.BookingDate,
                amount = m.Amount,
                currency = m.CurrencyCode,
                description = m.Description,
                counterparty = m.CounterpartyName,
                category = m.Category,
                isEssentialExpense = m.IsEssentialExpense,
                accountId = m.FinancialAccountId
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccount(Guid accountId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var account = await _dbContext.FinancialAccounts
            .Where(a => a.Id == accountId && a.UserId == userId)
            .Select(a => new
            {
                id = a.Id,
                displayName = a.DisplayName,
                provider = a.Provider,
                iban = a.Iban,
                balance = a.CurrentBalance,
                currency = a.CurrencyCode,
                lastSynced = a.LastSyncedAt,
                createdAt = a.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        return Ok(account);
    }

    [HttpGet("{accountId}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var account = await _dbContext.FinancialAccounts
            .Where(a => a.Id == accountId && a.UserId == userId)
            .Select(a => a.DisplayName)
            .FirstOrDefaultAsync();

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        var query = _dbContext.MoneyMovements
            .Where(m => m.FinancialAccountId == accountId);

        if (from.HasValue)
        {
            query = query.Where(m => m.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.TransactionDate <= to.Value);
        }

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(m => m.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                id = m.Id,
                transactionId = m.TransactionId,
                date = m.TransactionDate,
                bookingDate = m.BookingDate,
                amount = m.Amount,
                currency = m.CurrencyCode,
                description = m.Description,
                counterparty = m.CounterpartyName,
                category = m.Category,
                isEssentialExpense = m.IsEssentialExpense,
                accountId = accountId,
                accountName = account
            })
            .ToListAsync();

        return Ok(new
        {
            transactions,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpDelete("{accountId}")]
    public async Task<IActionResult> DeleteAccount(Guid accountId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var account = await _dbContext.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        _dbContext.FinancialAccounts.Remove(account);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Account deleted successfully" });
    }

    [HttpPut("{accountId}/rename")]
    public async Task<IActionResult> RenameAccount(Guid accountId, [FromBody] RenameAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            return BadRequest(new { error = "New name is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var account = await _dbContext.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        _logger.LogInformation("Renaming account {AccountId} from '{OldName}' to '{NewName}'", 
            accountId, account.DisplayName, request.NewName);

        account.DisplayName = request.NewName.Trim();
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Account renamed successfully", displayName = account.DisplayName });
    }

    [HttpPut("transactions/{transactionId}/toggle-essential")]
    public async Task<IActionResult> ToggleEssentialExpense(Guid transactionId, [FromBody] ToggleEssentialExpenseRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var transaction = await _dbContext.MoneyMovements
            .Include(m => m.Account)
            .FirstOrDefaultAsync(m => m.Id == transactionId && m.Account!.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new { error = "Transaction not found" });
        }

        transaction.IsEssentialExpense = request.IsEssential;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} essential expense flag set to {IsEssential}", 
            transactionId, request.IsEssential);

        return Ok(new { 
            message = "Essential expense flag updated successfully", 
            isEssentialExpense = transaction.IsEssentialExpense 
        });
    }

    [HttpPut("transactions/bulk-toggle-essential")]
    public async Task<IActionResult> BulkToggleEssentialExpense([FromBody] BulkToggleEssentialExpenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Description is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find all transactions with matching description (case-insensitive) for current user
        var transactions = await _dbContext.MoneyMovements
            .Include(m => m.Account)
            .Where(m => m.Description != null && m.Description.ToLower() == request.Description.ToLower()
                && m.Account!.UserId == userId)
            .ToListAsync();

        if (transactions.Count == 0)
        {
            return Ok(new { count = 0, message = "No matching transactions found" });
        }

        // Update all matching transactions
        foreach (var transaction in transactions)
        {
            transaction.IsEssentialExpense = request.IsEssential;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Bulk updated {Count} transactions with description '{Description}' to IsEssential={IsEssential}", 
            transactions.Count, request.Description, request.IsEssential);

        return Ok(new { 
            count = transactions.Count,
            message = $"Updated {transactions.Count} transaction(s) successfully"
        });
    }

    [HttpPost("manual-transaction")]
    public async Task<IActionResult> AddManualTransaction([FromBody] ManualTransactionRequest request)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(request.AccountId);
        
        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Description is required" });
        }

        // Parse the date
        if (!DateTime.TryParse(request.Date, out var transactionDate))
        {
            return BadRequest(new { error = "Invalid date format" });
        }

        // Create transaction ID from date and description
        var transactionId = $"{transactionDate:yyyyMMdd}_{request.Description}_{request.Amount}".GetHashCode().ToString();

        // Check if transaction already exists
        var exists = await _dbContext.MoneyMovements
            .AnyAsync(m => m.FinancialAccountId == request.AccountId && m.TransactionId == transactionId);

        if (exists)
        {
            return BadRequest(new { error = "A transaction with the same date, description, and amount already exists" });
        }

        var transaction = new Entities.MoneyMovement
        {
            Id = Guid.NewGuid(),
            FinancialAccountId = request.AccountId,
            TransactionId = transactionId,
            TransactionDate = transactionDate.ToUniversalTime(),
            BookingDate = transactionDate.ToUniversalTime(),
            Amount = request.Amount,
            CurrencyCode = "EUR",
            Description = request.Description,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MoneyMovements.Add(transaction);

        // Update account balance
        account.CurrentBalance += request.Amount;
        account.LastSyncedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Added manual transaction to account {AccountId}: {Amount:C} - {Description}", 
            request.AccountId, request.Amount, request.Description);

        return Ok(new
        {
            message = "Transaction added successfully",
            transactionId = transaction.Id,
            newBalance = account.CurrentBalance
        });
    }

    [HttpPut("{accountId}/exclude-from-total")]
    public async Task<IActionResult> ToggleExcludeFromTotal(Guid accountId, [FromBody] ExcludeFromTotalRequest request)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        
        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        account.ExcludeFromTotal = request.ExcludeFromTotal;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated account {AccountId} ExcludeFromTotal to {ExcludeFromTotal}", 
            accountId, request.ExcludeFromTotal);

        return Ok(new
        {
            message = "Account updated successfully",
            excludeFromTotal = account.ExcludeFromTotal
        });
    }

    [HttpPost("{accountId}/set-api-key")]
    public async Task<IActionResult> SetApiKey(Guid accountId, [FromBody] SetApiKeyRequest request)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        if (account == null)
        {
            return NotFound(new { message = "Account not found" });
        }

        // Store encrypted API key if provided, otherwise use plaintext (legacy)
        if (!string.IsNullOrEmpty(request.ApiKeyEncrypted))
        {
            account.ApiKeyEncrypted = request.ApiKeyEncrypted;
            account.ApiKey = null; // Clear legacy plaintext
        }
        else
        {
            account.ApiKey = request.ApiKey;
        }
        
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("API key set for account {AccountId}", accountId);

        return Ok(new { message = "API key saved successfully" });
    }

    [HttpPost("{accountId}/sync-trading212")]
    public async Task<IActionResult> SyncTrading212(Guid accountId)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        if (account == null)
        {
            return NotFound(new { message = "Account not found" });
        }

        if (string.IsNullOrEmpty(account.ApiKey))
        {
            return BadRequest(new { message = "API key not set for this account" });
        }

        // Fetch balance
        var cashBalance = await _trading212Service.GetCashBalanceAsync(account.ApiKey);
        if (cashBalance == null)
        {
            return BadRequest(new { message = "Failed to fetch balance from Trading 212. Check your API key." });
        }

        account.CurrentBalance = cashBalance.Value;
        account.LastSyncedAt = DateTime.UtcNow;

        // Also fetch transactions for future CSV mapping (they won't affect balance)
        var transactions = await _trading212Service.GetTransactionsAsync(account.ApiKey);
        var importedCount = 0;
        var linkedCount = 0;

        if (transactions != null && transactions.Count > 0)
        {
            foreach (var t in transactions)
            {
                // Use Reference (UUID) as the unique identifier
                var uniqueId = !string.IsNullOrEmpty(t.Reference) ? t.Reference : t.Id.ToString();
                var transactionId = $"t212-api-{uniqueId}";
                
                // Check if already exists by API ID
                var existsByApiId = await _dbContext.MoneyMovements
                    .AnyAsync(m => m.TransactionId == transactionId && m.FinancialAccountId == account.Id);
                
                if (existsByApiId)
                {
                    continue; // Already imported from API
                }

                // Try to find matching CSV transaction by ExternalId (UUID)
                var matchingCsvTransaction = await _dbContext.MoneyMovements
                    .FirstOrDefaultAsync(m => m.FinancialAccountId == account.Id 
                        && m.ExternalId == uniqueId);

                if (matchingCsvTransaction != null)
                {
                    // Link the existing CSV transaction to the API transaction
                    await _trading212MappingService.LinkApiToExistingTransaction(matchingCsvTransaction, uniqueId);
                    linkedCount++;
                }
                else
                {
                    // No CSV match found, create new transaction from API (for future mapping)
                    var movement = _trading212MappingService.CreateFromApiTransaction(account.Id, t);
                    movement.Id = Guid.NewGuid();
                    movement.CreatedAt = DateTime.UtcNow;
                    
                    _dbContext.MoneyMovements.Add(movement);
                    importedCount++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Synced Trading212 account {AccountId}, balance: {Balance}, imported {Imported} transactions, linked {Linked}", 
            accountId, cashBalance.Value, importedCount, linkedCount);

        var message = importedCount > 0 || linkedCount > 0
            ? $"Balance synced: {cashBalance.Value:C}\n{importedCount} new transactions imported\n{linkedCount} transactions linked to CSV"
            : $"Balance synced: {cashBalance.Value:C}\nNo new transactions";

        return Ok(new 
        { 
            message = message,
            balance = cashBalance.Value,
            lastSynced = account.LastSyncedAt,
            transactionsImported = importedCount,
            transactionsLinked = linkedCount
        });
    }

    [HttpPost("{accountId}/sync-trading212-encrypted")]
    public async Task<IActionResult> SyncTrading212Encrypted(Guid accountId, [FromBody] SyncWithEncryptedKeyRequest request)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        if (account == null)
        {
            return NotFound(new { message = "Account not found" });
        }

        if (string.IsNullOrEmpty(request.DecryptedApiKey))
        {
            return BadRequest(new { message = "Decrypted API key is required" });
        }

        // Use the provided decrypted API key (decrypted client-side)
        var cashBalance = await _trading212Service.GetCashBalanceAsync(request.DecryptedApiKey);
        if (cashBalance == null)
        {
            return BadRequest(new { message = "Failed to fetch balance from Trading 212. Check your API key." });
        }

        account.CurrentBalance = cashBalance.Value;

        // Fetch transactions
        var transactions = await _trading212Service.GetTransactionsAsync(request.DecryptedApiKey);
        if (transactions == null)
        {
            return BadRequest(new { message = "Failed to fetch transactions from Trading 212" });
        }

        // Import transactions (same logic as existing endpoint)
        int importedCount = 0;
        int linkedCount = 0;

        foreach (var apiTxn in transactions)
        {
            var movement = _trading212MappingService.CreateFromApiTransaction(accountId, apiTxn);
            
            var exists = await _dbContext.MoneyMovements
                .AnyAsync(m => m.FinancialAccountId == accountId && 
                             m.TransactionId == apiTxn.Reference);

            if (!exists)
            {
                var csvMatch = await _dbContext.MoneyMovements
                    .FirstOrDefaultAsync(m => 
                        m.FinancialAccountId == accountId &&
                        m.TransactionDate == movement.TransactionDate &&
                        Math.Abs(m.Amount - movement.Amount) < 0.01m &&
                        string.IsNullOrEmpty(m.TransactionId));

                if (csvMatch != null)
                {
                    csvMatch.TransactionId = apiTxn.Reference;
                    linkedCount++;
                }
                else
                {
                    movement.Id = Guid.NewGuid();
                    movement.CreatedAt = DateTime.UtcNow;
                    _dbContext.MoneyMovements.Add(movement);
                    importedCount++;
                }
            }
        }

        account.LastSyncedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Synced Trading212 account {AccountId} with encrypted key, balance: {Balance}, imported {Imported} transactions", 
            accountId, cashBalance.Value, importedCount);

        return Ok(new 
        { 
            message = $"Balance synced: {cashBalance.Value:C}\n{importedCount} new transactions imported\n{linkedCount} transactions linked",
            balance = cashBalance.Value,
            lastSynced = account.LastSyncedAt,
            transactionsImported = importedCount,
            transactionsLinked = linkedCount
        });
    }

    [HttpPost("{accountId}/sync-trading212-transactions")]
    public async Task<IActionResult> SyncTrading212Transactions(Guid accountId)
    {
        var account = await _dbContext.FinancialAccounts.FindAsync(accountId);
        if (account == null)
        {
            return NotFound(new { message = "Account not found" });
        }

        if (string.IsNullOrEmpty(account.ApiKey))
        {
            return BadRequest(new { message = "API key not set for this account" });
        }

        var transactions = await _trading212Service.GetTransactionsAsync(account.ApiKey);
        if (transactions == null)
        {
            return BadRequest(new { message = "Failed to fetch transactions from Trading 212" });
        }

        var importedCount = 0;
        var linkedCount = 0;
        
        foreach (var t in transactions)
        {
            // Use Reference (UUID) as the unique identifier
            var uniqueId = !string.IsNullOrEmpty(t.Reference) ? t.Reference : t.Id.ToString();
            var transactionId = $"t212-api-{uniqueId}";
            
            // Check if already exists by API ID
            var existsByApiId = await _dbContext.MoneyMovements
                .AnyAsync(m => m.TransactionId == transactionId && m.FinancialAccountId == account.Id);
            
            if (existsByApiId)
            {
                continue; // Already imported from API
            }

            // Try to find matching CSV transaction by ExternalId (UUID)
            var matchingCsvTransaction = await _dbContext.MoneyMovements
                .FirstOrDefaultAsync(m => m.FinancialAccountId == account.Id 
                    && m.ExternalId == uniqueId);

            if (matchingCsvTransaction != null)
            {
                // Link the existing CSV transaction to the API transaction
                await _trading212MappingService.LinkApiToExistingTransaction(matchingCsvTransaction, uniqueId);
                linkedCount++;
                _logger.LogInformation("Linked API transaction {ApiId} to CSV transaction {CsvId}",
                    uniqueId, matchingCsvTransaction.ExternalId);
            }
            else
            {
                // No CSV match found, create new transaction from API
                var movement = _trading212MappingService.CreateFromApiTransaction(account.Id, t);
                movement.Id = Guid.NewGuid();
                movement.CreatedAt = DateTime.UtcNow;
                
                _dbContext.MoneyMovements.Add(movement);
                importedCount++;
                _logger.LogInformation("Created new transaction from API: {ApiId}", uniqueId);
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Synced {ImportedCount} new and linked {LinkedCount} Trading212 transactions for account {AccountId}", 
            importedCount, linkedCount, accountId);

        return Ok(new 
        { 
            message = $"Synced {importedCount} new transactions and linked {linkedCount} existing CSV transactions",
            totalFetched = transactions.Count,
            imported = importedCount,
            linked = linkedCount
        });
    }

    [HttpPut("transactions/{transactionId}/description")]
    public async Task<IActionResult> UpdateTransactionDescription(Guid transactionId, [FromBody] UpdateDescriptionRequest request)
    {
        var transaction = await _dbContext.MoneyMovements.FindAsync(transactionId);
        if (transaction == null)
        {
            return NotFound(new { message = "Transaction not found" });
        }

        transaction.Description = request.Description;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated description for transaction {TransactionId}", transactionId);

        return Ok(new { message = "Description updated successfully" });
    }

    [HttpPut("transactions/{transactionId}/category/single")]
    public async Task<IActionResult> UpdateSingleTransactionCategory(Guid transactionId, [FromBody] UpdateCategoryRequest request)
    {
        var transaction = await _dbContext.MoneyMovements.FindAsync(transactionId);
        if (transaction == null)
        {
            return NotFound(new { message = "Transaction not found" });
        }

        transaction.Category = request.Category;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated category for single transaction {TransactionId} to {Category}", 
            transactionId, request.Category);

        return Ok(new { 
            message = "Category updated successfully",
            category = request.Category
        });
    }

    [HttpPut("transactions/{transactionId}/category")]
    public async Task<IActionResult> UpdateTransactionCategory(Guid transactionId, [FromBody] UpdateCategoryRequest request)
    {
        var transaction = await _dbContext.MoneyMovements.FindAsync(transactionId);
        if (transaction == null)
        {
            return NotFound(new { message = "Transaction not found" });
        }

        transaction.Category = request.Category;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated category for transaction {TransactionId} to {Category}", 
            transactionId, request.Category);

        // Learn from this categorization and apply to similar transactions
        var similarCount = await _categoryLearningService.LearnFromManualCategorizationAsync(transactionId, request.Category);

        return Ok(new { 
            message = "Category updated successfully",
            updatedCount = similarCount,
            category = request.Category
        });
    }

    [HttpPost("recategorize-all")]
    public async Task<IActionResult> RecategorizeAllTransactions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var transactions = await _dbContext.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .ToListAsync();
        var updated = 0;

        foreach (var transaction in transactions)
        {
            // Re-categorize if: no category, "Other", generic "Income", or generic categories
            var shouldRecategorize = string.IsNullOrEmpty(transaction.Category) || 
                                    transaction.Category == "Other" ||
                                    transaction.Category == "Income" ||
                                    transaction.Category == "Expenses" ||
                                    transaction.Category == "Large Expense";

            if (shouldRecategorize)
            {
                var (newCategory, markAsEssential) = await _categorizationService.CategorizeTransactionWithDetailsAsync(
                    userId,
                    transaction.Description ?? "",
                    transaction.CounterpartyName,
                    transaction.Amount);

                if (transaction.Category != newCategory)
                {
                    transaction.Category = newCategory;
                    transaction.IsEssentialExpense = markAsEssential;
                    updated++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Re-categorized {Count} transactions for user {UserId}", updated, userId);

        return Ok(new 
        { 
            message = $"Re-categorized {updated} transactions",
            total = transactions.Count,
            updated = updated
        });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = _categorizationService.GetAllCategories();
        return Ok(categories);
    }
}

public class RenameAccountRequest
{
    public string NewName { get; set; } = string.Empty;
}

public class ExcludeFromTotalRequest
{
    public bool ExcludeFromTotal { get; set; }
}

public class ManualTransactionRequest
{
    public Guid AccountId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = "Other";
}

public class SetApiKeyRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string? ApiKeyEncrypted { get; set; }
}

public class UpdateDescriptionRequest
{
    public string Description { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public string Category { get; set; } = string.Empty;
}

public class ToggleEssentialExpenseRequest
{
    public bool IsEssential { get; set; }
}

public class BulkToggleEssentialExpenseRequest
{
    public string Description { get; set; } = string.Empty;
    public bool IsEssential { get; set; }
}

public class SyncWithEncryptedKeyRequest
{
    public string DecryptedApiKey { get; set; } = string.Empty;
}
