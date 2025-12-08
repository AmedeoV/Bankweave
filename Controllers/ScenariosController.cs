using Bankweave.Infrastructure;
using Bankweave.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            var scenarios = await _dbContext.WhatIfScenarios
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
    public async Task<IActionResult> CreateScenario([FromBody] WhatIfScenario scenario)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scenario.Name))
            {
                return BadRequest("Scenario name is required");
            }

            // Check for duplicate name
            var existingScenario = await _dbContext.WhatIfScenarios
                .FirstOrDefaultAsync(s => s.Name == scenario.Name);
            
            if (existingScenario != null)
            {
                return Conflict("A scenario with this name already exists");
            }

            scenario.Id = Guid.NewGuid();
            scenario.CreatedAt = DateTime.UtcNow;
            scenario.UpdatedAt = DateTime.UtcNow;

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
    public async Task<IActionResult> UpdateScenario(Guid id, [FromBody] WhatIfScenario scenario)
    {
        try
        {
            if (id != scenario.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existingScenario = await _dbContext.WhatIfScenarios.FindAsync(id);
            
            if (existingScenario == null)
            {
                return NotFound($"Scenario with ID {id} not found");
            }

            // Check for duplicate name (excluding current scenario)
            var duplicateName = await _dbContext.WhatIfScenarios
                .AnyAsync(s => s.Name == scenario.Name && s.Id != id);
            
            if (duplicateName)
            {
                return Conflict("A scenario with this name already exists");
            }

            existingScenario.Name = scenario.Name;
            existingScenario.Description = scenario.Description;
            existingScenario.SavedDate = scenario.SavedDate;
            existingScenario.DateRangeStart = scenario.DateRangeStart;
            existingScenario.DateRangeEnd = scenario.DateRangeEnd;
            existingScenario.Days = scenario.Days;
            existingScenario.CustomTransactionsJson = scenario.CustomTransactionsJson;
            existingScenario.DisabledTransactionsJson = scenario.DisabledTransactionsJson;
            existingScenario.StatsJson = scenario.StatsJson;
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
