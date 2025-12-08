using System.Text.RegularExpressions;

namespace Bankweave.Services;

public class TransactionCategorizationService
{
    private readonly CategoryLearningService _learningService;
    private readonly RuleBasedCategorizationService _ruleService;

    public TransactionCategorizationService(
        CategoryLearningService learningService,
        RuleBasedCategorizationService ruleService)
    {
        _learningService = learningService;
        _ruleService = ruleService;
    }

    private static readonly Dictionary<string, List<string>> CategoryKeywords = new()
    {
        ["Groceries"] = new() { "tesco", "lidl", "aldi", "supervalu", "dunnes", "centra", "spar", "costcutter", "londis", "mace" },
        ["Restaurants & Dining"] = new() { "restaurant", "cafe", "coffee", "pub", "bar", "takeaway", "pizza", "burger", "food", "dining", "starbucks", "mcdonald", "kfc", "subway" },
        ["Transport"] = new() { "fuel", "petrol", "diesel", "gas station", "circle k", "topaz", "applegreen", "taxi", "uber", "bolt", "bus", "luas", "dart", "parking", "toll" },
        ["Shopping"] = new() { "amazon", "ebay", "shop", "store", "retail", "next", "zara", "h&m", "primark", "penneys", "argos", "harvey norman" },
        ["Entertainment"] = new() { "cinema", "theatre", "netflix", "spotify", "disney", "xbox", "playstation", "steam", "game", "concert", "ticket" },
        ["Utilities"] = new() { "electric ireland", "bord gais", "energia", "vodafone", "three", "eir", "virgin media", "sky", "water", "bin", "waste" },
        ["Healthcare"] = new() { "pharmacy", "boots", "doctor", "hospital", "clinic", "dentist", "vhi", "laya", "glohealth", "medical" },
        ["Travel"] = new() { "ryanair", "aer lingus", "hotel", "booking.com", "airbnb", "hostel", "flight", "airline" },
        ["Transfers"] = new() { "transfer", "revolut", "paypal", "bank transfer", "deposit" },
        ["Cash Withdrawal"] = new() { "atm", "withdrawal", "cash" },
        ["Salary"] = new() { "salary", "wages", "payroll", "income" },
        ["Bills"] = new() { "insurance", "tax", "revenue", "motor tax", "subscription", "membership" },
        ["Investments"] = new() { "trading 212", "degiro", "etoro", "trade republic", "stock", "crypto", "bitcoin" },
        ["Clothing"] = new() { "clothing", "clothes", "fashion", "patagonia", "north face", "nike", "adidas" },
        ["Education"] = new() { "school", "college", "university", "course", "udemy", "coursera", "books" },
        ["Home & Garden"] = new() { "furniture", "ikea", "homebase", "woodies", "b&q", "hardware", "garden" },
        ["Hello Fresh"] = new() { "hello fresh", "hellofresh" },
        ["Interests"] = new() { "interest", "interest earned", "interest paid" },
        ["Cashback"] = new() { "cashback", "cash back", "reward", "refund" }
    };

    public async Task<string> CategorizeTransactionAsync(string description, string? counterpartyName, decimal amount)
    {
        var result = await CategorizeTransactionWithDetailsAsync(description, counterpartyName, amount);
        return result.Category;
    }

    public async Task<(string Category, bool MarkAsEssential)> CategorizeTransactionWithDetailsAsync(string description, string? counterpartyName, decimal amount)
    {
        // Priority 1: Check user-defined rules (highest priority - overrides everything)
        var ruleMatch = await _ruleService.CategorizeByRulesWithDetailsAsync(description, amount);
        if (ruleMatch != null && !string.IsNullOrEmpty(ruleMatch.Category))
        {
            return (ruleMatch.Category, ruleMatch.MarkAsEssential);
        }

        // Priority 2: Check if we have a learned category for this transaction
        var learnedCategory = await _learningService.GetLearnedCategoryAsync(counterpartyName ?? "", description);
        if (!string.IsNullOrEmpty(learnedCategory))
        {
            return (learnedCategory, false);
        }

        // Priority 3: Fall back to keyword-based categorization
        return (CategorizeTransaction(description, counterpartyName, amount), false);
    }

    public string CategorizeTransaction(string description, string? counterpartyName, decimal amount)
    {
        // Combine description and counterparty for better matching
        var searchText = $"{description} {counterpartyName}".ToLower();

        // Check for income vs expense
        if (amount > 0)
        {
            if (ContainsKeywords(searchText, CategoryKeywords["Salary"]))
                return "Salary";
            if (ContainsKeywords(searchText, CategoryKeywords["Transfers"]))
                return "Transfer In";
            return "Income";
        }

        // Special handling for cash withdrawals
        if (ContainsKeywords(searchText, CategoryKeywords["Cash Withdrawal"]))
            return "Cash Withdrawal";

        // Check each category
        foreach (var category in CategoryKeywords)
        {
            if (category.Key == "Salary" || category.Key == "Cash Withdrawal")
                continue; // Already handled

            if (ContainsKeywords(searchText, category.Value))
                return category.Key;
        }

        // Default categories based on amount patterns
        if (amount < -500)
            return "Large Expense";
        if (amount < -100)
            return "Bills";
        
        return "Other";
    }

    private bool ContainsKeywords(string text, List<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> GetAllCategories()
    {
        return new List<string>
        {
            "Groceries",
            "Restaurants & Dining",
            "Transport",
            "Shopping",
            "Entertainment",
            "Utilities",
            "Healthcare",
            "Travel",
            "Transfers",
            "Transfer In",
            "Cash Withdrawal",
            "Salary",
            "Bills",
            "Investments",
            "Clothing",
            "Education",
            "Home & Garden",
            "Interests",
            "Cashback",
            "Income",
            "Large Expense",
            "Other"
        };
    }
}
