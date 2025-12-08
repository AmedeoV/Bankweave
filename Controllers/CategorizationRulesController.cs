using Bankweave.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategorizationRulesController : ControllerBase
{
    private readonly RuleBasedCategorizationService _ruleService;
    private readonly ILogger<CategorizationRulesController> _logger;

    public CategorizationRulesController(
        RuleBasedCategorizationService ruleService,
        ILogger<CategorizationRulesController> logger)
    {
        _ruleService = ruleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var rules = await _ruleService.GetRulesAsync(userId);
        return Ok(rules);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pattern))
        {
            return BadRequest(new { error = "Pattern is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { error = "Category is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var rule = await _ruleService.AddRuleAsync(
            userId,
            request.Pattern,
            request.Category,
            request.IsRegex,
            request.CaseSensitive,
            request.Priority,
            request.MarkAsEssential,
            request.TransactionType);

        return Ok(rule);
    }

    [HttpPut("{ruleId}")]
    public async Task<IActionResult> UpdateRule(Guid ruleId, [FromBody] UpdateRuleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var rule = await _ruleService.UpdateRuleAsync(
            userId,
            ruleId,
            request.Pattern,
            request.Category,
            request.IsRegex,
            request.CaseSensitive,
            request.Priority,
            request.MarkAsEssential,
            request.TransactionType);

        if (rule == null)
        {
            return NotFound(new { error = "Rule not found" });
        }

        return Ok(rule);
    }

    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> DeleteRule(Guid ruleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var success = await _ruleService.DeleteRuleAsync(userId, ruleId);
        
        if (!success)
        {
            return NotFound(new { error = "Rule not found" });
        }

        return Ok(new { message = "Rule deleted successfully" });
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyRulesToExisting()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var count = await _ruleService.ApplyRulesToExistingTransactionsAsync(userId);
        return Ok(new { 
            message = $"Applied rules to {count} transactions",
            count = count
        });
    }
}

public class CreateRuleRequest
{
    public string Pattern { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRegex { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
    public int Priority { get; set; } = 0;
    public bool MarkAsEssential { get; set; } = false;
    public string TransactionType { get; set; } = "Any";
}

public class UpdateRuleRequest
{
    public string? Pattern { get; set; }
    public string? Category { get; set; }
    public bool? IsRegex { get; set; }
    public bool? CaseSensitive { get; set; }
    public int? Priority { get; set; }
    public bool? MarkAsEssential { get; set; }
    public string? TransactionType { get; set; }
}
