namespace Bankweave.Entities;

public class FinancialAccount
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "EUR";
    public decimal CurrentBalance { get; set; }
    public decimal StartingBalance { get; set; }
    public bool ExcludeFromTotal { get; set; } = false;
    public bool IsCreditCard { get; set; } = false;
    public string? ApiKey { get; set; }
    public string? ApiKeyEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? RequisitionId { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
    public ICollection<MoneyMovement> Movements { get; set; } = new List<MoneyMovement>();
}
