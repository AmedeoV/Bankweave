using Bankweave.Entities;
using Bankweave.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bankweave.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .Select(u => new
            {
                id = u.Id,
                email = u.Email,
                firstName = u.FirstName,
                lastName = u.LastName,
                emailConfirmed = u.EmailConfirmed,
                createdAt = u.CreatedAt,
                lockoutEnabled = u.LockoutEnabled,
                lockoutEnd = u.LockoutEnd
            })
            .ToListAsync();

        // Get roles for each user
        var usersWithRoles = new List<object>();
        foreach (var user in users)
        {
            var appUser = await _userManager.FindByIdAsync(user.id);
            var roles = await _userManager.GetRolesAsync(appUser!);
            
            usersWithRoles.Add(new
            {
                user.id,
                user.email,
                user.firstName,
                user.lastName,
                user.emailConfirmed,
                user.createdAt,
                user.lockoutEnabled,
                user.lockoutEnd,
                roles = roles.ToList()
            });
        }

        return Ok(usersWithRoles);
    }

    [HttpGet("users/{userId}/stats")]
    public async Task<IActionResult> GetUserStats(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var accountCount = await _context.FinancialAccounts
            .Where(a => a.UserId == userId)
            .CountAsync();

        var transactionCount = await _context.MoneyMovements
            .Where(m => m.Account.UserId == userId)
            .CountAsync();

        var ruleCount = await _context.CategorizationRules
            .Where(r => r.UserId == userId)
            .CountAsync();

        var scenarioCount = await _context.WhatIfScenarios
            .Where(s => s.UserId == userId)
            .CountAsync();

        return Ok(new
        {
            accountCount,
            transactionCount,
            ruleCount,
            scenarioCount
        });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Prevent deleting yourself
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == currentUserId)
            return BadRequest(new { message = "Cannot delete your own account" });

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation($"Admin deleted user {user.Email}");
            return Ok(new { message = "User deleted successfully" });
        }

        return BadRequest(new { message = "Failed to delete user", errors = result.Errors });
    }

    [HttpPost("users/{userId}/lock")]
    public async Task<IActionResult> LockUser(string userId, [FromBody] LockUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Prevent locking yourself
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == currentUserId)
            return BadRequest(new { message = "Cannot lock your own account" });

        var lockoutEnd = dto.Permanent ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddDays(dto.Days ?? 7);
        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Admin locked user {user.Email} until {lockoutEnd}");
            return Ok(new { message = "User locked successfully", lockoutEnd });
        }

        return BadRequest(new { message = "Failed to lock user", errors = result.Errors });
    }

    [HttpPost("users/{userId}/unlock")]
    public async Task<IActionResult> UnlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Admin unlocked user {user.Email}");
            return Ok(new { message = "User unlocked successfully" });
        }

        return BadRequest(new { message = "Failed to unlock user", errors = result.Errors });
    }

    [HttpPost("users/{userId}/roles/{roleName}")]
    public async Task<IActionResult> AddRoleToUser(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation($"Admin added role {roleName} to user {user.Email}");
                return Ok(new { message = $"Role {roleName} added successfully" });
            }
            return BadRequest(new { message = "Failed to add role", errors = result.Errors });
        }

        return BadRequest(new { message = "User already has this role" });
    }

    [HttpDelete("users/{userId}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRoleFromUser(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Prevent removing admin role from yourself
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == currentUserId && roleName.ToLower() == "admin")
            return BadRequest(new { message = "Cannot remove admin role from your own account" });

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation($"Admin removed role {roleName} from user {user.Email}");
                return Ok(new { message = $"Role {roleName} removed successfully" });
            }
            return BadRequest(new { message = "Failed to remove role", errors = result.Errors });
        }

        return BadRequest(new { message = "User doesn't have this role" });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles.Select(r => r.Name).ToListAsync();
        return Ok(roles);
    }
}

public class LockUserDto
{
    public bool Permanent { get; set; }
    public int? Days { get; set; }
}
