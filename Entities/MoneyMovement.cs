namespace Bankweave.Entities;

public class MoneyMovement
{
    public Guid Id { get; set; }
    public Guid FinancialAccountId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? ExternalId { get; set; }  // For storing external IDs like Trading212 CSV UUIDs
    public DateTime TransactionDate { get; set; }
    public DateTime BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string Description { get; set; } = string.Empty;
    public string? CounterpartyName { get; set; }
    public string? Category { get; set; }
    public bool IsEssentialExpense { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    
    // Encrypted fields for zero-knowledge privacy
    public string? DescriptionEncrypted { get; set; }
    public string? CounterpartyNameEncrypted { get; set; }
    public string? AmountEncrypted { get; set; }
    public string? CategoryEncrypted { get; set; }
    
    public FinancialAccount Account { get; set; } = null!;
}
