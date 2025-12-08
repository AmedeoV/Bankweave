using Bankweave.Entities;
using System.Globalization;

namespace Bankweave.Services.CsvParsers;

public class GenericCsvParser : ICsvParser
{
    protected readonly ILogger<GenericCsvParser> _logger;

    public GenericCsvParser(ILogger<GenericCsvParser>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCsvParser>.Instance;
    }

    public virtual CsvParseResult Parse(string csvContent, string accountName)
    {
        var transactions = new List<MoneyMovement>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _logger.LogInformation("GenericCsvParser: Processing {LineCount} lines for account {AccountName}", lines.Length, accountName);

        if (lines.Length < 2)
        {
            _logger.LogWarning("GenericCsvParser: CSV has less than 2 lines");
            return new CsvParseResult { Transactions = transactions };
        }

        // Skip header
        int successCount = 0;
        int failCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            _logger.LogDebug("GenericCsvParser: Line {LineNum} parsed into {PartCount} parts", i, parts.Length);

            if (parts.Length < 3)
            {
                _logger.LogDebug("GenericCsvParser: Skipping line {LineNum} - insufficient parts", i);
                failCount++;
                continue;
            }

            try
            {
                var date = DateTime.SpecifyKind(DateTime.Parse(parts[0]), DateTimeKind.Utc);
                var description = parts.Length > 1 ? parts[1] : "";
                var amount = decimal.Parse(parts[2], CultureInfo.InvariantCulture);

                transactions.Add(new MoneyMovement
                {
                    TransactionId = $"csv-{Guid.NewGuid()}",
                    TransactionDate = date,
                    BookingDate = date,
                    Amount = amount,
                    Description = description,
                    CurrencyCode = "EUR"
                });

                successCount++;
                _logger.LogDebug("GenericCsvParser: Successfully parsed transaction - Date: {Date}, Amount: {Amount}", date, amount);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("GenericCsvParser: Failed to parse line {LineNum}: {Error}", i, ex.Message);
                failCount++;
                continue;
            }
        }

        _logger.LogInformation("GenericCsvParser: Parsed {SuccessCount} transactions, {FailCount} failures", successCount, failCount);
        return new CsvParseResult 
        { 
            Transactions = transactions,
            HasBalanceColumn = false
        };
    }

    protected string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result.ToArray();
    }
}
