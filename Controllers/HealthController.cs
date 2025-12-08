using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bankweave.Infrastructure;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Try to query the database
            await _context.Database.CanConnectAsync();
            
            // Get applied migrations
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            
            return Ok(new
            {
                status = "connected",
                appliedMigrations = appliedMigrations.ToList(),
                pendingMigrations = pendingMigrations.ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("migrate")]
    public async Task<IActionResult> RunMigrations()
    {
        try
        {
            await _context.Database.MigrateAsync();
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            
            return Ok(new
            {
                status = "success",
                message = "Migrations applied successfully",
                appliedMigrations = appliedMigrations.ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message,
                stackTrace = ex.StackTrace,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
