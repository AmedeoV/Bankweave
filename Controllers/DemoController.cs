using Bankweave.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly DemoDataSeederService _seederService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(DemoDataSeederService seederService, ILogger<DemoController> logger)
    {
        _seederService = seederService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a demo account with sample data for testing and demonstration purposes.
    /// This endpoint can be called without authentication.
    /// </summary>
    /// <returns>Details of the created demo account including credentials</returns>
    [HttpPost("create-demo-account")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateDemoAccount()
    {
        try
        {
            _logger.LogInformation("Creating demo account...");
            
            var (success, message, userId) = await _seederService.CreateDemoAccountAsync();
            
            if (!success)
            {
                _logger.LogError("Failed to create demo account: {Message}", message);
                return BadRequest(new { error = message });
            }

            _logger.LogInformation("Demo account created successfully. UserId: {UserId}", userId);
            
            return Ok(new
            {
                success = true,
                message = message,
                userId = userId,
                credentials = new
                {
                    email = "demo@bankweave.app",
                    password = "Demo123!"
                },
                accounts = new[]
                {
                    "Main Checking Account - EUR",
                    "Savings Account - EUR",
                    "Visa Credit Card - EUR",
                    "Investment Account (Trading212) - EUR"
                },
                stats = new
                {
                    transactionsCount = "100+",
                    timeRange = "Last 3 months",
                    categories = new[] { "Income", "Housing", "Groceries", "Dining", "Transportation", "Entertainment", "Shopping", "Healthcare", "Investment" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating demo account");
            return StatusCode(500, new { error = "An error occurred while creating the demo account", details = ex.Message });
        }
    }

    /// <summary>
    /// Get demo account information without creating a new one.
    /// Useful for checking if demo account exists.
    /// </summary>
    [HttpGet("demo-info")]
    [AllowAnonymous]
    public IActionResult GetDemoInfo()
    {
        return Ok(new
        {
            email = "demo@bankweave.app",
            password = "Demo123!",
            description = "This is a fully functional demo account with sample financial data",
            features = new[]
            {
                "Multiple account types (Checking, Savings, Credit Card, Investment)",
                "90+ realistic transactions spanning 3 months",
                "Automatic categorization with pre-configured rules",
                "Income, expenses, and investment tracking",
                "Real-world spending patterns"
            },
            instructions = new[]
            {
                "1. Use POST /api/demo/create-demo-account to create/reset the demo account",
                "2. Login with the credentials above at POST /api/auth/login",
                "3. Explore all features of Bankweave with realistic data"
            }
        });
    }
}
