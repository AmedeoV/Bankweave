using Bankweave.Entities;

namespace Bankweave.Services.CsvParsers;

public interface ICsvParser
{
    CsvParseResult Parse(string csvContent, string accountName);
}

public class CsvParseResult
{
    public List<MoneyMovement> Transactions { get; set; } = new();
    public decimal? DetectedBalance { get; set; }
    public bool HasBalanceColumn { get; set; }
}
