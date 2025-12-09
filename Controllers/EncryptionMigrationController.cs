using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Bankweave.Infrastructure;
using Bankweave.Entities;

namespace Bankweave.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EncryptionMigrationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EncryptionMigrationController> _logger;

    public EncryptionMigrationController(
        AppDbContext context,
        ILogger<EncryptionMigrationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<MigrationStatus>> GetMigrationStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var totalTransactions = await _context.MoneyMovements
            .Include(m => m.Account)
            .CountAsync(m => m.Account.UserId == userId);

        var encryptedTransactions = await _context.MoneyMovements
            .Include(m => m.Account)
            .CountAsync(m => m.Account.UserId == userId && 
                           m.DescriptionEncrypted != null);

        var unencryptedTransactions = totalTransactions - encryptedTransactions;
        
        // Check accounts
        var totalAccounts = await _context.FinancialAccounts
            .CountAsync(a => a.UserId == userId);
        var encryptedAccounts = await _context.FinancialAccounts
            .CountAsync(a => a.UserId == userId && a.DisplayNameEncrypted != null);
        
        // Check rules
        var totalRules = await _context.CategorizationRules
            .CountAsync(r => r.UserId == userId);
        var encryptedRules = await _context.CategorizationRules
            .CountAsync(r => r.UserId == userId && r.PatternEncrypted != null);
        
        // Check scenarios
        var totalScenarios = await _context.WhatIfScenarios
            .CountAsync(s => s.UserId == userId);
        var encryptedScenarios = await _context.WhatIfScenarios
            .CountAsync(s => s.UserId == userId && s.NameEncrypted != null);
        
        // Check user names
        var user = await _context.Users.FindAsync(userId);
        var userNamesEncrypted = user?.FirstNameEncrypted != null || user?.LastNameEncrypted != null;
        var userNamesNeedMigration = (user?.FirstName != null || user?.LastName != null) && !userNamesEncrypted;

        return Ok(new MigrationStatus
        {
            TotalTransactions = totalTransactions,
            EncryptedTransactions = encryptedTransactions,
            UnencryptedTransactions = unencryptedTransactions,
            TotalAccounts = totalAccounts,
            EncryptedAccounts = encryptedAccounts,
            UnencryptedAccounts = totalAccounts - encryptedAccounts,
            TotalRules = totalRules,
            EncryptedRules = encryptedRules,
            UnencryptedRules = totalRules - encryptedRules,
            TotalScenarios = totalScenarios,
            EncryptedScenarios = encryptedScenarios,
            UnencryptedScenarios = totalScenarios - encryptedScenarios,
            UserNamesEncrypted = userNamesEncrypted,
            UserNamesNeedMigration = userNamesNeedMigration,
            NeedsMigration = unencryptedTransactions > 0 || 
                            (totalAccounts - encryptedAccounts) > 0 ||
                            (totalRules - encryptedRules) > 0 ||
                            (totalScenarios - encryptedScenarios) > 0 ||
                            userNamesNeedMigration
        });
    }

    [HttpPost("migrate")]
    public async Task<ActionResult<MigrationResult>> MigrateTransactions([FromBody] MigrateTransactionsRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.EncryptedTransactions == null || request.EncryptedTransactions.Count == 0)
        {
            return BadRequest("No encrypted transactions provided");
        }

        var migratedCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        foreach (var encTxn in request.EncryptedTransactions)
        {
            try
            {
                var transaction = await _context.MoneyMovements
                    .Include(m => m.Account)
                    .FirstOrDefaultAsync(m => m.Id == encTxn.Id && m.Account.UserId == userId);

                if (transaction == null)
                {
                    failedCount++;
                    errors.Add($"Transaction {encTxn.Id} not found or access denied");
                    continue;
                }

                // Update encrypted fields
                transaction.DescriptionEncrypted = encTxn.DescriptionEncrypted;
                transaction.CounterpartyNameEncrypted = encTxn.CounterpartyNameEncrypted;
                transaction.AmountEncrypted = encTxn.AmountEncrypted;
                transaction.CategoryEncrypted = encTxn.CategoryEncrypted;

                migratedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"Transaction {encTxn.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to migrate transaction {TransactionId}", encTxn.Id);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new MigrationResult
        {
            MigratedCount = migratedCount,
            FailedCount = failedCount,
            Errors = errors
        });
    }

    [HttpPost("migrate-all")]
    public async Task<ActionResult<MigrationResult>> MigrateAll([FromBody] MigrateAllRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var totalMigrated = 0;
        var totalFailed = 0;
        var errors = new List<string>();

        // Migrate transactions
        if (request.EncryptedTransactions?.Count > 0)
        {
            foreach (var encTxn in request.EncryptedTransactions)
            {
                try
                {
                    var transaction = await _context.MoneyMovements
                        .Include(m => m.Account)
                        .FirstOrDefaultAsync(m => m.Id == encTxn.Id && m.Account.UserId == userId);

                    if (transaction != null)
                    {
                        transaction.DescriptionEncrypted = encTxn.DescriptionEncrypted;
                        transaction.CounterpartyNameEncrypted = encTxn.CounterpartyNameEncrypted;
                        transaction.AmountEncrypted = encTxn.AmountEncrypted;
                        transaction.CategoryEncrypted = encTxn.CategoryEncrypted;
                        totalMigrated++;
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    _logger.LogError(ex, "Failed to migrate transaction {TransactionId}", encTxn.Id);
                }
            }
        }

        // Migrate accounts
        if (request.EncryptedAccounts?.Count > 0)
        {
            foreach (var encAcct in request.EncryptedAccounts)
            {
                try
                {
                    var account = await _context.FinancialAccounts
                        .FirstOrDefaultAsync(a => a.Id == encAcct.Id && a.UserId == userId);

                    if (account != null)
                    {
                        account.DisplayNameEncrypted = encAcct.DisplayNameEncrypted;
                        totalMigrated++;
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    _logger.LogError(ex, "Failed to migrate account {AccountId}", encAcct.Id);
                }
            }
        }

        // Migrate rules
        if (request.EncryptedRules?.Count > 0)
        {
            foreach (var encRule in request.EncryptedRules)
            {
                try
                {
                    var rule = await _context.CategorizationRules
                        .FirstOrDefaultAsync(r => r.Id == encRule.Id && r.UserId == userId);

                    if (rule != null)
                    {
                        rule.PatternEncrypted = encRule.PatternEncrypted;
                        totalMigrated++;
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    _logger.LogError(ex, "Failed to migrate rule {RuleId}", encRule.Id);
                }
            }
        }

        // Migrate scenarios
        if (request.EncryptedScenarios?.Count > 0)
        {
            foreach (var encScen in request.EncryptedScenarios)
            {
                try
                {
                    var scenario = await _context.WhatIfScenarios
                        .FirstOrDefaultAsync(s => s.Id == encScen.Id && s.UserId == userId);

                    if (scenario != null)
                    {
                        scenario.NameEncrypted = encScen.NameEncrypted;
                        scenario.DescriptionEncrypted = encScen.DescriptionEncrypted;
                        scenario.CustomTransactionsJsonEncrypted = encScen.CustomTransactionsJsonEncrypted;
                        scenario.DisabledTransactionsJsonEncrypted = encScen.DisabledTransactionsJsonEncrypted;
                        scenario.StatsJsonEncrypted = encScen.StatsJsonEncrypted;
                        totalMigrated++;
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    _logger.LogError(ex, "Failed to migrate scenario {ScenarioId}", encScen.Id);
                }
            }
        }

        // Migrate user names
        if (request.EncryptedUserNames != null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.FirstNameEncrypted = request.EncryptedUserNames.FirstNameEncrypted;
                    user.LastNameEncrypted = request.EncryptedUserNames.LastNameEncrypted;
                    totalMigrated++;
                }
            }
            catch (Exception ex)
            {
                totalFailed++;
                _logger.LogError(ex, "Failed to migrate user names");
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new MigrationResult
        {
            MigratedCount = totalMigrated,
            FailedCount = totalFailed,
            Errors = errors
        });
    }
}

public class MigrateAllRequest
{
    public List<EncryptedTransactionDto>? EncryptedTransactions { get; set; }
    public List<EncryptedAccountDto>? EncryptedAccounts { get; set; }
    public List<EncryptedRuleDto>? EncryptedRules { get; set; }
    public List<EncryptedScenarioDto>? EncryptedScenarios { get; set; }
    public EncryptedUserNamesDto? EncryptedUserNames { get; set; }
}

public class EncryptedAccountDto
{
    public Guid Id { get; set; }
    public string? DisplayNameEncrypted { get; set; }
}

public class EncryptedRuleDto
{
    public Guid Id { get; set; }
    public string? PatternEncrypted { get; set; }
}

public class EncryptedScenarioDto
{
    public Guid Id { get; set; }
    public string? NameEncrypted { get; set; }
    public string? DescriptionEncrypted { get; set; }
    public string? CustomTransactionsJsonEncrypted { get; set; }
    public string? DisabledTransactionsJsonEncrypted { get; set; }
    public string? StatsJsonEncrypted { get; set; }
}

public class EncryptedUserNamesDto
{
    public string? FirstNameEncrypted { get; set; }
    public string? LastNameEncrypted { get; set; }
}

public class MigrationStatus
{
    public int TotalTransactions { get; set; }
    public int EncryptedTransactions { get; set; }
    public int UnencryptedTransactions { get; set; }
    public int TotalAccounts { get; set; }
    public int EncryptedAccounts { get; set; }
    public int UnencryptedAccounts { get; set; }
    public int TotalRules { get; set; }
    public int EncryptedRules { get; set; }
    public int UnencryptedRules { get; set; }
    public int TotalScenarios { get; set; }
    public int EncryptedScenarios { get; set; }
    public int UnencryptedScenarios { get; set; }
    public bool UserNamesEncrypted { get; set; }
    public bool UserNamesNeedMigration { get; set; }
    public bool NeedsMigration { get; set; }
}

public class MigrateTransactionsRequest
{
    public List<EncryptedTransactionDto> EncryptedTransactions { get; set; } = new();
}

public class EncryptedTransactionDto
{
    public Guid Id { get; set; }
    public string? DescriptionEncrypted { get; set; }
    public string? CounterpartyNameEncrypted { get; set; }
    public string? AmountEncrypted { get; set; }
    public string? CategoryEncrypted { get; set; }
}

public class MigrationResult
{
    public int MigratedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
