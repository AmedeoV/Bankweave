namespace Bankweave.Entities;

public class WhatIfScenario
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEncrypted { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEncrypted { get; set; }
    public DateTime SavedDate { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public int? Days { get; set; }
    public string CustomTransactionsJson { get; set; } = "[]";
    public string? CustomTransactionsJsonEncrypted { get; set; }
    public string DisabledTransactionsJson { get; set; } = "[]";
    public string? DisabledTransactionsJsonEncrypted { get; set; }
    public string StatsJson { get; set; } = "{}";
    public string? StatsJsonEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
}
