namespace Bankweave.Entities;

public class CategorizationRule
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRegex { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
    public int Priority { get; set; } = 0; // Higher priority rules are checked first
    public bool MarkAsEssential { get; set; } = false;
    public string TransactionType { get; set; } = "Any"; // "Any", "Positive", "Negative"
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int TimesUsed { get; set; } = 0;
    
    public ApplicationUser User { get; set; } = null!;
}
