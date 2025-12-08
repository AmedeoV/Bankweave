using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Bankweave.Services;

public class RuleMatchResult
{
    public string? Category { get; set; }
    public bool MarkAsEssential { get; set; }
}

public class RuleBasedCategorizationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RuleBasedCategorizationService> _logger;
    private List<CategorizationRule>? _cachedRules;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public RuleBasedCategorizationService(
        AppDbContext context, 
        ILogger<RuleBasedCategorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all categorization rules, ordered by priority (highest first)
    /// </summary>
    public async Task<List<CategorizationRule>> GetRulesAsync()
    {
        if (_cachedRules != null && DateTime.UtcNow - _cacheTime < _cacheExpiry)
        {
            return _cachedRules;
        }

        _cachedRules = await _context.CategorizationRules
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();
        
        _cacheTime = DateTime.UtcNow;
        return _cachedRules;
    }

    /// <summary>
    /// Invalidate the rules cache
    /// </summary>
    public void InvalidateCache()
    {
        _cachedRules = null;
    }

    /// <summary>
    /// Try to categorize a transaction based on rules
    /// Returns the category if a rule matches, null otherwise
    /// </summary>
    public async Task<string?> CategorizeByRulesAsync(string description, decimal? amount = null)
    {
        var result = await CategorizeByRulesWithDetailsAsync(description, amount);
        return result?.Category;
    }

    /// <summary>
    /// Try to categorize a transaction based on rules, returning full match details
    /// </summary>
    public async Task<RuleMatchResult?> CategorizeByRulesWithDetailsAsync(string description, decimal? amount = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var rules = await GetRulesAsync();

        foreach (var rule in rules)
        {
            // Check transaction type filter
            if (amount.HasValue && rule.TransactionType != "Any")
            {
                if (rule.TransactionType == "Positive" && amount.Value <= 0)
                    continue;
                if (rule.TransactionType == "Negative" && amount.Value >= 0)
                    continue;
            }

            bool matches = false;

            try
            {
                if (rule.IsRegex)
                {
                    var options = rule.CaseSensitive 
                        ? RegexOptions.None 
                        : RegexOptions.IgnoreCase;
                    matches = Regex.IsMatch(description, rule.Pattern, options);
                }
                else
                {
                    var comparison = rule.CaseSensitive 
                        ? StringComparison.Ordinal 
                        : StringComparison.OrdinalIgnoreCase;
                    matches = description.Contains(rule.Pattern, comparison);
                }

                if (matches)
                {
                    // Update usage statistics
                    rule.LastUsedAt = DateTime.UtcNow;
                    rule.TimesUsed++;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Transaction categorized by rule: '{Description}' matched pattern '{Pattern}' -> {Category}",
                        description, rule.Pattern, rule.Category);

                    return new RuleMatchResult
                    {
                        Category = rule.Category,
                        MarkAsEssential = rule.MarkAsEssential
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying rule {RuleId} with pattern '{Pattern}'", 
                    rule.Id, rule.Pattern);
            }
        }

        return null;
    }

    /// <summary>
    /// Apply rules to ALL existing transactions (overrides current categories)
    /// </summary>
    public async Task<int> ApplyRulesToExistingTransactionsAsync()
    {
        var transactions = await _context.MoneyMovements
            .ToListAsync();

        int categorizedCount = 0;

        foreach (var transaction in transactions)
        {
            var result = await CategorizeByRulesWithDetailsAsync(transaction.Description ?? "", transaction.Amount);
            if (result != null && !string.IsNullOrEmpty(result.Category))
            {
                transaction.Category = result.Category;
                transaction.IsEssentialExpense = result.MarkAsEssential;
                categorizedCount++;
            }
        }

        if (categorizedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Applied rules to {Count} existing transactions", categorizedCount);
        }

        return categorizedCount;
    }

    /// <summary>
    /// Add a new categorization rule
    /// </summary>
    public async Task<CategorizationRule> AddRuleAsync(
        string pattern, 
        string category, 
        bool isRegex = false, 
        bool caseSensitive = false,
        int priority = 0,
        bool markAsEssential = false,
        string transactionType = "Any")
    {
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Pattern = pattern,
            Category = category,
            IsRegex = isRegex,
            CaseSensitive = caseSensitive,
            Priority = priority,
            MarkAsEssential = markAsEssential,
            TransactionType = transactionType,
            CreatedAt = DateTime.UtcNow
        };

        _context.CategorizationRules.Add(rule);
        await _context.SaveChangesAsync();
        
        InvalidateCache();
        _logger.LogInformation("Created categorization rule: '{Pattern}' -> {Category}", pattern, category);

        return rule;
    }

    /// <summary>
    /// Delete a categorization rule
    /// </summary>
    public async Task<bool> DeleteRuleAsync(Guid ruleId)
    {
        var rule = await _context.CategorizationRules.FindAsync(ruleId);
        if (rule == null)
        {
            return false;
        }

        _context.CategorizationRules.Remove(rule);
        await _context.SaveChangesAsync();
        
        InvalidateCache();
        _logger.LogInformation("Deleted categorization rule: '{Pattern}'", rule.Pattern);

        return true;
    }

    /// <summary>
    /// Update an existing rule
    /// </summary>
    public async Task<CategorizationRule?> UpdateRuleAsync(
        Guid ruleId,
        string? pattern = null,
        string? category = null,
        bool? isRegex = null,
        bool? caseSensitive = null,
        int? priority = null,
        bool? markAsEssential = null,
        string? transactionType = null)
    {
        var rule = await _context.CategorizationRules.FindAsync(ruleId);
        if (rule == null)
        {
            return null;
        }

        if (pattern != null) rule.Pattern = pattern;
        if (category != null) rule.Category = category;
        if (isRegex != null) rule.IsRegex = isRegex.Value;
        if (caseSensitive != null) rule.CaseSensitive = caseSensitive.Value;
        if (priority != null) rule.Priority = priority.Value;
        if (markAsEssential != null) rule.MarkAsEssential = markAsEssential.Value;
        if (transactionType != null) rule.TransactionType = transactionType;

        await _context.SaveChangesAsync();
        InvalidateCache();
        
        _logger.LogInformation("Updated categorization rule {RuleId}", ruleId);
        return rule;
    }
}
