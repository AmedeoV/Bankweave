namespace Bankweave.Entities;

public class BalanceSnapshot
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public decimal TotalBalance { get; set; }
    
    // Optional: Store breakdown by account for detailed analysis
    public string? AccountBalances { get; set; } // JSON string of account balances
    public string? AccountBalancesEncrypted { get; set; }
}
