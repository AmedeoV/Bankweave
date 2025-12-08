using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class TradeRepublicCsvParser : GenericCsvParser
{
    private readonly ILogger<TradeRepublicCsvParser> _specificLogger;

    public TradeRepublicCsvParser(ILogger<TradeRepublicCsvParser>? logger = null) : base(logger)
    {
        _specificLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TradeRepublicCsvParser>.Instance;
    }

    public override CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _specificLogger.LogInformation("TradeRepublicParser: Processing {LineCount} lines", lines.Length);

        if (lines.Length < 2) return new CsvParseResult { Transactions = transactions };

        // Trade Republic format may vary, adapt as needed
        int successCount = 0;
        int failCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 4)
            {
                failCount++;
                continue;
            }

            try
            {
                var dateStr = parts[0];
                DateTime date;
                if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                {
                    date = DateTime.Parse(dateStr);
                }
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                
                var description = parts[1];
                var amount = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
                var currency = parts.Length > 3 ? parts[3] : "EUR";

                transactions.Add(new MoneyMovement
                {
                    TransactionId = $"tr-{date:yyyyMMdd}-{Guid.NewGuid()}",
                    TransactionDate = date,
                    BookingDate = date,
                    Amount = amount,
                    Description = description,
                    CurrencyCode = currency,
                    Category = "Investment"
                });

                successCount++;
            }
            catch (Exception ex)
            {
                _specificLogger.LogDebug("TradeRepublicParser: Failed line {LineNum}: {Error}", i, ex.Message);
                failCount++;
                continue;
            }
        }

        _specificLogger.LogInformation("TradeRepublicParser: Parsed {SuccessCount} transactions, {FailCount} failures", successCount, failCount);
        return new CsvParseResult { Transactions = transactions, HasBalanceColumn = false };
    }
}
