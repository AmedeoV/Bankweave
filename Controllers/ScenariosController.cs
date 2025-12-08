using Bankweave.Infrastructure;
using Bankweave.Entities;
using Bankweave.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScenariosController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(AppDbContext dbContext, ILogger<ScenariosController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // GET: api/scenarios
    [HttpGet]
    public async Task<IActionResult> GetAllScenarios()
    {
        try
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var scenarios = await _dbContext.WhatIfScenarios
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedDate)
                .ToListAsync();
            
            return Ok(scenarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenarios");
            return StatusCode(500, "Error retrieving scenarios");
        }
    }

    // GET: api/scenarios/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScenario(Guid id)
    {
        try
        {
            var scenario = await _dbContext.WhatIfScenarios.FindAsync(id);
            
            if (scenario == null)
            {
                return NotFound($"Scenario with ID {id} not found");
            }

            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify the scenario belongs to the current user
            if (scenario.UserId != userId)
            {
                return Forbid();
            }
            
            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenario {ScenarioId}", id);
            return StatusCode(500, "Error retrieving scenario");
        }
    }

    // POST: api/scenarios
    [HttpPost]
    public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Scenario name is required");
            }

            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check for duplicate name for this user
            var existingScenario = await _dbContext.WhatIfScenarios
                .FirstOrDefaultAsync(s => s.Name == dto.Name && s.UserId == userId);
            
            if (existingScenario != null)
            {
                return Conflict("A scenario with this name already exists");
            }

            var scenario = new WhatIfScenario
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description,
                SavedDate = dto.SavedDate,
                DateRangeStart = dto.DateRangeStart,
                DateRangeEnd = dto.DateRangeEnd,
                Days = dto.Days,
                CustomTransactionsJson = dto.CustomTransactionsJson,
                DisabledTransactionsJson = dto.DisabledTransactionsJson,
                StatsJson = dto.StatsJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.WhatIfScenarios.Add(scenario);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetScenario), new { id = scenario.Id }, scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, "Error creating scenario");
        }
    }

    // PUT: api/scenarios/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScenario(Guid id, [FromBody] UpdateScenarioDto dto)
    {
        try
        {
            var existingScenario = await _dbContext.WhatIfScenarios.FindAsync(id);
            
            if (existingScenario == null)
            {
                return NotFound($"Scenario with ID {id} not found");
            }

            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify the scenario belongs to the current user
            if (existingScenario.UserId != userId)
            {
                return Forbid();
            }

            // Check for duplicate name (excluding current scenario)
            var duplicateName = await _dbContext.WhatIfScenarios
                .AnyAsync(s => s.Name == dto.Name && s.Id != id && s.UserId == userId);
            
            if (duplicateName)
            {
                return Conflict("A scenario with this name already exists");
            }

            existingScenario.Name = dto.Name;
            existingScenario.Description = dto.Description;
            existingScenario.SavedDate = dto.SavedDate;
            existingScenario.DateRangeStart = dto.DateRangeStart;
            existingScenario.DateRangeEnd = dto.DateRangeEnd;
            existingScenario.Days = dto.Days;
            existingScenario.CustomTransactionsJson = dto.CustomTransactionsJson;
            existingScenario.DisabledTransactionsJson = dto.DisabledTransactionsJson;
            existingScenario.StatsJson = dto.StatsJson;
            existingScenario.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(existingScenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scenario {ScenarioId}", id);
            return StatusCode(500, "Error updating scenario");
        }
    }

    // DELETE: api/scenarios/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScenario(Guid id)
    {
        try
        {
            var scenario = await _dbContext.WhatIfScenarios.FindAsync(id);
            
            if (scenario == null)
            {
                return NotFound($"Scenario with ID {id} not found");
            }

            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify the scenario belongs to the current user
            if (scenario.UserId != userId)
            {
                return Forbid();
            }

            _dbContext.WhatIfScenarios.Remove(scenario);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", id);
            return StatusCode(500, "Error deleting scenario");
        }
    }
}
