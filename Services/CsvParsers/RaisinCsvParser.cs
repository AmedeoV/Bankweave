using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class RaisinCsvParser : GenericCsvParser
{
    private readonly ILogger<RaisinCsvParser> _specificLogger;

    public RaisinCsvParser(ILogger<RaisinCsvParser>? logger = null) : base(logger)
    {
        _specificLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RaisinCsvParser>.Instance;
    }

    public override CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _specificLogger.LogInformation("RaisinParser: Processing {LineCount} lines", lines.Length);

        if (lines.Length < 2) return new CsvParseResult { Transactions = transactions };

        // Raisin format: adapt based on actual CSV structure
        int successCount = 0;
        int failCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 3)
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

                transactions.Add(new MoneyMovement
                {
                    TransactionId = $"raisin-{date:yyyyMMdd}-{Guid.NewGuid()}",
                    TransactionDate = date,
                    BookingDate = date,
                    Amount = amount,
                    Description = description,
                    CurrencyCode = "EUR",
                    Category = "Savings"
                });

                successCount++;
            }
            catch (Exception ex)
            {
                _specificLogger.LogDebug("RaisinParser: Failed line {LineNum}: {Error}", i, ex.Message);
                failCount++;
                continue;
            }
        }

        _specificLogger.LogInformation("RaisinParser: Parsed {SuccessCount} transactions, {FailCount} failures", successCount, failCount);
        return new CsvParseResult { Transactions = transactions, HasBalanceColumn = false };
    }
}
