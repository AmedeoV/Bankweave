using System.Text.Json;

namespace Bankweave.Services;

public class Trading212ApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Trading212ApiService> _logger;
    private const string BaseUrl = "https://live.trading212.com/api/v0";

    public Trading212ApiService(IHttpClientFactory httpClientFactory, ILogger<Trading212ApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal?> GetCashBalanceAsync(string apiKey)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            // Trading 212 expects API_KEY:API_SECRET in Base64 with Basic auth
            // apiKey is stored as "key:secret" format
            _logger.LogInformation("API Key format: {KeyLength} characters, contains colon: {HasColon}", 
                apiKey.Length, apiKey.Contains(':'));
            
            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey));
            var authHeader = $"Basic {credentials}";
            
            _logger.LogInformation("Authorization header (first 20 chars): {AuthHeader}", 
                authHeader.Length > 20 ? authHeader.Substring(0, 20) + "..." : authHeader);
            
            client.DefaultRequestHeaders.Add("Authorization", authHeader);

            _logger.LogInformation("Attempting to fetch Trading212 balance from: {Url}", $"{BaseUrl}/equity/account/cash");

            var response = await client.GetAsync($"{BaseUrl}/equity/account/cash");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Trading212 API error: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            _logger.LogInformation("Trading212 API response: {Response}", responseContent);

            var data = JsonSerializer.Deserialize<Trading212CashResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null)
            {
                _logger.LogError("Failed to deserialize Trading212 response");
                return null;
            }

            _logger.LogInformation("Successfully fetched Trading212 balance: {Balance}", data.Total);
            return data.Total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Trading212 cash balance");
            return null;
        }
    }

    public async Task<List<Trading212Transaction>?> GetTransactionsAsync(string apiKey, int limit = 50)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            var url = $"{BaseUrl}/equity/history/transactions?limit={limit}";
            _logger.LogInformation("Fetching Trading212 transactions from: {Url}", url);

            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Trading212 transactions API error: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            _logger.LogInformation("Trading212 transactions response received");

            var data = JsonSerializer.Deserialize<Trading212TransactionsResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return data?.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Trading212 transactions");
            return null;
        }
    }
}

public class Trading212CashResponse
{
    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Ppl { get; set; }
    public decimal Result { get; set; }
    public decimal Invested { get; set; }
}

public class Trading212TransactionsResponse
{
    public List<Trading212Transaction> Items { get; set; } = new();
    public string? NextPagePath { get; set; }
}

public class Trading212Transaction
{
    public long Id { get; set; }
    public DateTime DateTime { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}
