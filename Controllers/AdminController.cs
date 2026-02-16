using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Data;
using chat_dotnet.Models;
using chat_dotnet.Models.DTOs;

namespace chat_dotnet.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FullName,
                u.IsOnline,
                u.LastSeen,
                u.CreatedAt
            })
            .ToListAsync();

        var totalUsers = await _context.Users.CountAsync();

        return Ok(new
        {
            users,
            totalUsers,
            totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
            currentPage = page
        });
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.IsOnline,
            user.LastSeen,
            user.CreatedAt,
            Roles = roles
        });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation($"User deleted: {user.UserName}");

        return Ok(new { message = "User deleted successfully" });
    }

    [HttpPost("users/{userId}/roles")]
    public async Task<IActionResult> AddRoleToUser(string userId, [FromBody] string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation($"Role {role} added to user {user.UserName}");

        return Ok(new { message = $"Role {role} added successfully" });
    }

    [HttpDelete("users/{userId}/roles/{role}")]
    public async Task<IActionResult> RemoveRoleFromUser(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation($"Role {role} removed from user {user.UserName}");

        return Ok(new { message = $"Role {role} removed successfully" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalUsers = await _context.Users.CountAsync();
        var onlineUsers = await _context.Users.CountAsync(u => u.IsOnline);
        var totalMessages = await _context.Messages.CountAsync();
        var totalGroups = await _context.Groups.CountAsync();
        var totalConversations = await _context.Conversations.CountAsync();

        var recentUsers = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new { u.UserName, u.CreatedAt })
            .ToListAsync();

        return Ok(new
        {
            totalUsers,
            onlineUsers,
            totalMessages,
            totalGroups,
            totalConversations,
            recentUsers
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetAllConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var conversations = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                User1 = c.User1.UserName,
                User2 = c.User2.UserName,
                c.CreatedAt,
                c.LastMessageAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetAllGroups([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var groups = await _context.Groups
            .Include(g => g.CreatedBy)
            .Include(g => g.Members)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CreatedById = g.CreatedById,
                CreatedByName = g.CreatedBy.UserName ?? string.Empty,
                CreatedAt = g.CreatedAt,
                MemberCount = g.Members.Count
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpDelete("groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
        {
            return NotFound();
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Group deleted by admin: {group.Name}");

        return Ok(new { message = "Group deleted successfully" });
    }
}
