using Microsoft.AspNetCore.Identity;

namespace Bankweave.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? FirstNameEncrypted { get; set; }
    public string? LastName { get; set; }
    public string? LastNameEncrypted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Encryption salt for deriving user's encryption key from password
    public string? EncryptionSalt { get; set; }
    
    // Navigation properties
    public ICollection<FinancialAccount> FinancialAccounts { get; set; } = new List<FinancialAccount>();
    public ICollection<CategorizationRule> CategorizationRules { get; set; } = new List<CategorizationRule>();
    public ICollection<WhatIfScenario> WhatIfScenarios { get; set; } = new List<WhatIfScenario>();
}
