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
public class EncryptedTransactionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EncryptedTransactionsController> _logger;

    public EncryptedTransactionsController(
        AppDbContext context,
        ILogger<EncryptedTransactionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEncryptedTransactions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var transactions = await _context.MoneyMovements
            .Include(m => m.Account)
            .Where(m => m.Account.UserId == userId)
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new
            {
                m.Id,
                m.FinancialAccountId,
                m.TransactionId,
                m.ExternalId,
                m.TransactionDate,
                m.BookingDate,
                m.CurrencyCode,
                m.DescriptionEncrypted,
                m.CounterpartyNameEncrypted,
                m.AmountEncrypted,
                m.CategoryEncrypted,
                m.IsEssentialExpense,
                m.CreatedAt,
                AccountName = m.Account.DisplayName,
                AccountProvider = m.Account.Provider
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetEncryptedTransaction(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var transaction = await _context.MoneyMovements
            .Include(m => m.Account)
            .Where(m => m.Id == id && m.Account.UserId == userId)
            .Select(m => new
            {
                m.Id,
                m.FinancialAccountId,
                m.TransactionId,
                m.ExternalId,
                m.TransactionDate,
                m.BookingDate,
                m.CurrencyCode,
                m.DescriptionEncrypted,
                m.CounterpartyNameEncrypted,
                m.AmountEncrypted,
                m.CategoryEncrypted,
                m.IsEssentialExpense,
                m.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateEncryptedTransaction(CreateEncryptedTransactionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify account belongs to user
        var account = await _context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == dto.FinancialAccountId && a.UserId == userId);

        if (account == null)
        {
            return BadRequest("Invalid account");
        }

        var transaction = new MoneyMovement
        {
            Id = Guid.NewGuid(),
            FinancialAccountId = dto.FinancialAccountId,
            TransactionId = Guid.NewGuid().ToString(),
            TransactionDate = dto.TransactionDate,
            BookingDate = dto.BookingDate,
            CurrencyCode = dto.CurrencyCode ?? "EUR",
            DescriptionEncrypted = dto.DescriptionEncrypted,
            CounterpartyNameEncrypted = dto.CounterpartyNameEncrypted,
            AmountEncrypted = dto.AmountEncrypted,
            CategoryEncrypted = dto.CategoryEncrypted,
            IsEssentialExpense = dto.IsEssentialExpense,
            CreatedAt = DateTime.UtcNow
        };

        _context.MoneyMovements.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEncryptedTransaction), new { id = transaction.Id }, new
        {
            transaction.Id,
            transaction.FinancialAccountId,
            transaction.TransactionId,
            transaction.TransactionDate,
            transaction.BookingDate,
            transaction.CurrencyCode,
            transaction.DescriptionEncrypted,
            transaction.CounterpartyNameEncrypted,
            transaction.AmountEncrypted,
            transaction.CategoryEncrypted,
            transaction.IsEssentialExpense,
            transaction.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEncryptedTransaction(Guid id, UpdateEncryptedTransactionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var transaction = await _context.MoneyMovements
            .Include(m => m.Account)
            .FirstOrDefaultAsync(m => m.Id == id && m.Account.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        transaction.DescriptionEncrypted = dto.DescriptionEncrypted ?? transaction.DescriptionEncrypted;
        transaction.CounterpartyNameEncrypted = dto.CounterpartyNameEncrypted ?? transaction.CounterpartyNameEncrypted;
        transaction.AmountEncrypted = dto.AmountEncrypted ?? transaction.AmountEncrypted;
        transaction.CategoryEncrypted = dto.CategoryEncrypted ?? transaction.CategoryEncrypted;
        transaction.IsEssentialExpense = dto.IsEssentialExpense;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEncryptedTransaction(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var transaction = await _context.MoneyMovements
            .Include(m => m.Account)
            .FirstOrDefaultAsync(m => m.Id == id && m.Account.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        _context.MoneyMovements.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateEncryptedTransactionDto
{
    public Guid FinancialAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime BookingDate { get; set; }
    public string? CurrencyCode { get; set; }
    public string? DescriptionEncrypted { get; set; }
    public string? CounterpartyNameEncrypted { get; set; }
    public string? AmountEncrypted { get; set; }
    public string? CategoryEncrypted { get; set; }
    public bool IsEssentialExpense { get; set; }
}

public class UpdateEncryptedTransactionDto
{
    public string? DescriptionEncrypted { get; set; }
    public string? CounterpartyNameEncrypted { get; set; }
    public string? AmountEncrypted { get; set; }
    public string? CategoryEncrypted { get; set; }
    public bool IsEssentialExpense { get; set; }
}
