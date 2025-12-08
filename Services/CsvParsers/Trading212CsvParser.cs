using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class Trading212CsvParser : GenericCsvParser
{
    private readonly ILogger<Trading212CsvParser> _specificLogger;

    public Trading212CsvParser(ILogger<Trading212CsvParser>? logger = null) : base(logger)
    {
        _specificLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Trading212CsvParser>.Instance;
    }

    public override CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _specificLogger.LogInformation("Trading212Parser: Processing {LineCount} lines for account {AccountName}", lines.Length, accountName);

        if (lines.Length < 2)
        {
            _specificLogger.LogWarning("Trading212Parser: CSV has less than 2 lines");
            return new CsvParseResult { Transactions = transactions };
        }

        // Trading 212 format: Action,Time,Notes,ID,Total,Currency (Total),Currency conversion fee,Currency (Currency conversion fee),Merchant name,Merchant category
        _specificLogger.LogDebug("Trading212Parser: Header: {Header}", lines[0]);

        int successCount = 0;
        int failCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            _specificLogger.LogDebug("Trading212Parser: Line {LineNum} has {PartCount} parts: [{Parts}]", 
                i, parts.Length, string.Join("] [", parts.Take(6)));

            // Be more lenient - only need Action, Time, ID, Total, Currency
            if (parts.Length < 5)
            {
                _specificLogger.LogWarning("Trading212Parser: Line {LineNum} has only {PartCount} parts, need at least 5", i, parts.Length);
                failCount++;
                continue;
            }

            try
            {
                var action = parts[0];
                var timeStr = parts[1];
                DateTime time;
                if (!DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time))
                {
                    time = DateTime.Parse(timeStr);
                }
                time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                
                var notes = parts.Length > 2 ? parts[2] : "";
                var id = parts.Length > 3 ? parts[3] : "";
                
                // Extract UUID from notes if it's in "WITHDRAW: uuid" or "DEPOSIT: uuid" format
                string? extractedId = null;
                if (!string.IsNullOrEmpty(notes))
                {
                    var colonIndex = notes.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < notes.Length - 1)
                    {
                        extractedId = notes.Substring(colonIndex + 1).Trim();
                        _specificLogger.LogDebug("Trading212Parser: Extracted UUID from notes: {ExtractedId}", extractedId);
                    }
                }
                
                // Use extracted ID from notes, fallback to ID column, then generate new
                var finalId = !string.IsNullOrEmpty(extractedId) ? extractedId : 
                              (!string.IsNullOrEmpty(id) && id != "0" ? id : 
                              Guid.NewGuid().ToString());
                
                // Parse amount - handle empty strings
                var totalStr = parts[4].Trim();
                if (string.IsNullOrWhiteSpace(totalStr))
                {
                    _specificLogger.LogDebug("Trading212Parser: Line {LineNum} has empty Total field", i);
                    failCount++;
                    continue;
                }
                
                var total = decimal.Parse(totalStr, CultureInfo.InvariantCulture);
                var currency = parts.Length > 5 ? parts[5] : "EUR";
                var merchantName = parts.Length > 8 ? parts[8] : "";

                var description = string.IsNullOrEmpty(merchantName) ? 
                    (string.IsNullOrEmpty(notes) ? action : $"{action} - {notes}") : 
                    $"{action} - {merchantName}";

                transactions.Add(new MoneyMovement
                {
                    TransactionId = $"t212-{finalId}",
                    ExternalId = finalId,  // Store the UUID for mapping with API transactions
                    TransactionDate = time,
                    BookingDate = time,
                    Amount = total,
                    Description = description,
                    CurrencyCode = currency,
                    Category = action.Contains("Interest") ? "Interest" : (action.Contains("Card") ? "Expense" : "Investment")
                });

                successCount++;
                _specificLogger.LogDebug("Trading212Parser: Successfully parsed - Action: {Action}, Amount: {Amount}, FinalId: {Id}", 
                    action, total, finalId);
            }
            catch (Exception ex)
            {
                _specificLogger.LogWarning("Trading212Parser: Failed to parse line {LineNum}: {Error} - Line content: {Line}", 
                    i, ex.Message, lines[i].Substring(0, Math.Min(100, lines[i].Length)));
                failCount++;
                continue;
            }
        }

        _specificLogger.LogInformation("Trading212Parser: Parsed {SuccessCount} transactions, {FailCount} failures", successCount, failCount);
        return new CsvParseResult { Transactions = transactions, HasBalanceColumn = false };
    }
}
