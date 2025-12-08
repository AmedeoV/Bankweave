using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class PtsbCsvParser : GenericCsvParser
{
    private readonly ILogger<PtsbCsvParser> _specificLogger;

    public PtsbCsvParser(ILogger<PtsbCsvParser>? logger = null) : base(logger)
    {
        _specificLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PtsbCsvParser>.Instance;
    }

    public override CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _specificLogger.LogInformation("PtsbParser: Processing {LineCount} lines for account {AccountName}", lines.Length, accountName);

        if (lines.Length < 2)
        {
            _specificLogger.LogWarning("PtsbParser: CSV has less than 2 lines");
            return new CsvParseResult { Transactions = transactions };
        }

        // PTSB format: "Date","Description","Money in (€)","Money out (€)","Balance (€)"
        _specificLogger.LogDebug("PtsbParser: Header: {Header}", lines[0]);

        int successCount = 0;
        int failCount = 0;
        int skippedEmptyDates = 0;
        decimal? lastBalance = null;
        var transactionSequence = new Dictionary<string, int>(); // Track sequence numbers for identical transactions

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            _specificLogger.LogDebug("PtsbParser: Line {LineNum} has {PartCount} parts", i, parts.Length);

            if (parts.Length < 4)
            {
                _specificLogger.LogDebug("PtsbParser: Line {LineNum} has insufficient parts", i);
                failCount++;
                continue;
            }

            try
            {
                // Extract balance from first row (even if date is empty) - this is the current balance
                if (lastBalance == null && parts.Length > 4)
                {
                    var balanceStr = parts[4].Replace(",", "").Replace("€", "").Trim();
                    if (!string.IsNullOrWhiteSpace(balanceStr) && decimal.TryParse(balanceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
                    {
                        lastBalance = balance;
                        _specificLogger.LogInformation("PtsbParser: Detected current balance from first row: {Balance:C}", balance);
                    }
                }
                
                var dateStr = parts[0].Trim();
                
                DateTime date;
                // If date is empty, use today's date for pending transactions
                if (string.IsNullOrWhiteSpace(dateStr))
                {
                    date = DateTime.UtcNow.Date;
                    skippedEmptyDates++;
                    _specificLogger.LogDebug("PtsbParser: Line {LineNum} has empty date - using today for pending transaction", i);
                }
                else
                {
                    // Parse date - PTSB uses "26 Nov 2025" format
                    if (!DateTime.TryParseExact(dateStr, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                    {
                        // Try alternative format
                        if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                        {
                            _specificLogger.LogWarning("PtsbParser: Line {LineNum} failed to parse date: {DateStr}", i, dateStr);
                            failCount++;
                            continue;
                        }
                    }
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                }

                var description = parts[1].Trim();
                var moneyIn = parts[2].Replace(",", "").Replace("€", "").Trim();
                var moneyOut = parts[3].Replace(",", "").Replace("€", "").Trim();

                decimal amount = 0;
                if (!string.IsNullOrWhiteSpace(moneyIn))
                {
                    var parsed = decimal.Parse(moneyIn, CultureInfo.InvariantCulture);
                    amount = Math.Abs(parsed); // Money in is always positive
                    _specificLogger.LogDebug("PtsbParser: Line {LineNum} - Money IN: {Amount}", i, amount);
                }
                else if (!string.IsNullOrWhiteSpace(moneyOut))
                {
                    var parsed = decimal.Parse(moneyOut, CultureInfo.InvariantCulture);
                    amount = -Math.Abs(parsed); // Money out is always negative
                    _specificLogger.LogDebug("PtsbParser: Line {LineNum} - Money OUT: {Amount}", i, amount);
                }
                else
                {
                    _specificLogger.LogDebug("PtsbParser: Line {LineNum} has no amount - skipping", i);
                    failCount++;
                    continue;
                }

                // Create a unique transaction ID that includes date, amount, and description
                // Add sequence number to handle multiple identical transactions on the same day
                var uniqueKey = $"{date:yyyyMMdd}-{amount:F2}-{description}";
                
                // Track sequence for duplicate transactions within the same CSV
                if (!transactionSequence.ContainsKey(uniqueKey))
                {
                    transactionSequence[uniqueKey] = 0;
                }
                var sequence = transactionSequence[uniqueKey]++;
                
                var transactionId = sequence == 0 
                    ? $"ptsb-{uniqueKey.GetHashCode():X8}" 
                    : $"ptsb-{uniqueKey.GetHashCode():X8}-{sequence}";
                
                transactions.Add(new MoneyMovement
                {
                    TransactionId = transactionId,
                    TransactionDate = date,
                    BookingDate = date,
                    Amount = amount,
                    Description = description,
                    CurrencyCode = "EUR",
                    Category = amount > 0 ? "Income" : "Expense"
                });

                successCount++;
                _specificLogger.LogDebug("PtsbParser: Successfully parsed - Date: {Date}, Amount: {Amount}, Desc: {Desc}", 
                    date.ToString("dd MMM yyyy"), amount, description.Substring(0, Math.Min(30, description.Length)));
            }
            catch (Exception ex)
            {
                _specificLogger.LogWarning("PtsbParser: Failed to parse line {LineNum}: {Error}", i, ex.Message);
                failCount++;
                continue;
            }
        }

        _specificLogger.LogInformation("PtsbParser: Parsed {SuccessCount} transactions, {FailCount} failures, {SkippedCount} empty dates", 
            successCount, failCount, skippedEmptyDates);
        
        if (lastBalance.HasValue)
        {
            _specificLogger.LogInformation("PtsbParser: Using balance from CSV as current balance: {Balance:C}", lastBalance.Value);
        }
        
        return new CsvParseResult
        {
            Transactions = transactions,
            DetectedBalance = lastBalance,
            HasBalanceColumn = true
        };
    }
}
