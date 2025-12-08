using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class RevolutCsvParser : GenericCsvParser
{
    private readonly ILogger<RevolutCsvParser> _specificLogger;

    public RevolutCsvParser(ILogger<RevolutCsvParser>? logger = null) : base(logger)
    {
        _specificLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RevolutCsvParser>.Instance;
    }

    public override CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _specificLogger.LogInformation("RevolutParser: Processing {LineCount} lines", lines.Length);
        
        if (lines.Length > 0)
        {
            _specificLogger.LogInformation("RevolutParser: Header line: {Header}", lines[0]);
        }

        if (lines.Length < 2) return new CsvParseResult { Transactions = transactions };

        // Detect format based on header
        var header = lines[0].ToLower();
        bool isCreditCardFormat = header.Contains("type,started date,completed date,description,amount,fee,balance");
        bool isRegularFormat = header.Contains("product");
        
        _specificLogger.LogInformation("RevolutParser: Detected format - Credit Card: {IsCreditCard}, Regular: {IsRegular}", 
            isCreditCardFormat, isRegularFormat);

        int successCount = 0;
        int failCount = 0;
        decimal? finalBalance = null;

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            
            _specificLogger.LogDebug("RevolutParser: Line {LineNum} has {PartCount} parts", i, parts.Length);
            
            // Credit card format has 7 columns, regular has 10
            int minColumns = isCreditCardFormat ? 5 : 6;
            
            if (parts.Length < minColumns)
            {
                _specificLogger.LogWarning("RevolutParser: Line {LineNum} has insufficient parts ({Count}), skipping", i, parts.Length);
                failCount++;
                continue;
            }

            try
            {
                DateTime date;
                string description;
                decimal amount;
                string currency = "EUR";
                
                if (isCreditCardFormat)
                {
                    // Credit card format: Type,Started Date,Completed Date,Description,Amount,Fee,Balance
                    var completedDate = parts[2]; // Index 2
                    
                    _specificLogger.LogDebug("RevolutParser: Line {LineNum} - Parsing date: {Date}", i, completedDate);
                    
                    if (!DateTime.TryParse(completedDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                    {
                        date = DateTime.Parse(completedDate);
                    }
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    
                    description = parts[3]; // Index 3
                    var amountStr = parts[4]; // Index 4
                    
                    _specificLogger.LogDebug("RevolutParser: Line {LineNum} - Amount string: '{Amount}'", i, amountStr);
                    
                    amount = decimal.Parse(amountStr, CultureInfo.InvariantCulture);
                    
                    // Extract balance from last column if available (index 6)
                    if (parts.Length > 6 && decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
                    {
                        finalBalance = balance;
                    }
                }
                else
                {
                    // Regular format: Type,Product,Started Date,Completed Date,Description,Amount,Fee,Currency,State,Balance
                    var completedDate = parts.Length > 3 ? parts[3] : parts[2];
                    
                    _specificLogger.LogDebug("RevolutParser: Line {LineNum} - Parsing date: {Date}", i, completedDate);
                    
                    if (!DateTime.TryParse(completedDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                    {
                        date = DateTime.Parse(completedDate);
                    }
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    
                    description = parts.Length > 4 ? parts[4] : "";
                    var amountStr = parts[5];
                    
                    _specificLogger.LogDebug("RevolutParser: Line {LineNum} - Amount string: '{Amount}'", i, amountStr);
                    
                    amount = decimal.Parse(amountStr, CultureInfo.InvariantCulture);
                    currency = parts.Length > 7 ? parts[7] : "EUR";

                    // Extract balance from last column if available
                    if (parts.Length > 9 && decimal.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
                    {
                        finalBalance = balance;
                    }
                }

                transactions.Add(new MoneyMovement
                {
                    TransactionId = $"revolut-{date:yyyyMMdd}-{Guid.NewGuid()}",
                    TransactionDate = date,
                    BookingDate = date,
                    Amount = amount,
                    Description = description,
                    CurrencyCode = currency
                });

                successCount++;
            }
            catch (Exception ex)
            {
                _specificLogger.LogWarning("RevolutParser: Failed line {LineNum}: {Error} - Line content: {Content}", 
                    i, ex.Message, lines[i].Length > 100 ? lines[i].Substring(0, 100) + "..." : lines[i]);
                failCount++;
                continue;
            }
        }

        _specificLogger.LogInformation("RevolutParser: Parsed {SuccessCount} transactions, {FailCount} failures", successCount, failCount);
        _specificLogger.LogInformation("RevolutParser: Detected final balance: {Balance}", finalBalance);
        return new CsvParseResult 
        { 
            Transactions = transactions, 
            HasBalanceColumn = finalBalance.HasValue,
            DetectedBalance = finalBalance
        };
    }
}
