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

        return Ok(new MigrationStatus
        {
            TotalTransactions = totalTransactions,
            EncryptedTransactions = encryptedTransactions,
            UnencryptedTransactions = unencryptedTransactions,
            NeedsMigration = unencryptedTransactions > 0
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
}

public class MigrationStatus
{
    public int TotalTransactions { get; set; }
    public int EncryptedTransactions { get; set; }
    public int UnencryptedTransactions { get; set; }
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
