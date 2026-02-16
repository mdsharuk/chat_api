using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Data;
using chat_dotnet.Models;
using chat_dotnet.Models.DTOs;

namespace chat_dotnet.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GroupController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GroupController> _logger;

    public GroupController(ApplicationDbContext context, ILogger<GroupController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var groups = await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Include(gm => gm.Group)
            .ThenInclude(g => g.CreatedBy)
            .Include(gm => gm.Group)
            .ThenInclude(g => g.Members)
            .Select(gm => new GroupDto
            {
                Id = gm.Group.Id,
                Name = gm.Group.Name,
                Description = gm.Group.Description,
                CreatedById = gm.Group.CreatedById,
                CreatedByName = gm.Group.CreatedBy.UserName ?? string.Empty,
                CreatedAt = gm.Group.CreatedAt,
                MemberCount = gm.Group.Members.Count
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroup(int groupId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Verify user is member
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        var group = await _context.Groups
            .Where(g => g.Id == groupId)
            .Include(g => g.CreatedBy)
            .Include(g => g.Members)
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
            .FirstOrDefaultAsync();

        if (group == null)
        {
            return NotFound();
        }

        return Ok(group);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto model)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var group = new Group
        {
            Name = model.Name,
            Description = model.Description,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Add creator as admin member
        var creatorMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };
        _context.GroupMembers.Add(creatorMember);

        // Add other members
        foreach (var memberId in model.MemberIds.Where(id => id != userId))
        {
            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = memberId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };
            _context.GroupMembers.Add(member);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Group created: {group.Name} by user {userId}");

        return CreatedAtAction(nameof(GetGroup), new { groupId = group.Id }, new { id = group.Id, name = group.Name });
    }

    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetGroupMembers(int groupId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Verify user is member
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        var members = await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User)
            .Select(gm => new GroupMemberDto
            {
                UserId = gm.UserId,
                Username = gm.User.UserName ?? string.Empty,
                FullName = gm.User.FullName,
                IsAdmin = gm.IsAdmin,
                IsOnline = gm.User.IsOnline,
                JoinedAt = gm.JoinedAt
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpGet("{groupId}/messages")]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Verify user is member
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        var messages = await _context.Messages
            .Where(m => m.GroupId == groupId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName ?? string.Empty,
                SentAt = m.SentAt,
                GroupId = m.GroupId
            })
            .ToListAsync();

        messages.Reverse();

        return Ok(messages);
    }

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] int newMemberId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Verify user is admin of group
        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsAdmin);

        if (!isAdmin)
        {
            return Forbid();
        }

        // Check if user is already a member
        var alreadyMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == newMemberId);

        if (alreadyMember)
        {
            return BadRequest("User is already a member");
        }

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = newMemberId,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Member added successfully" });
    }

    [HttpDelete("{groupId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(int groupId, int memberId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Verify user is admin of group
        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsAdmin);

        if (!isAdmin && userId != memberId)
        {
            return Forbid();
        }

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == memberId);

        if (member == null)
        {
            return NotFound();
        }

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Member removed successfully" });
    }

    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
        {
            return NotFound();
        }

        // Only creator can delete group
        if (group.CreatedById != userId)
        {
            return Forbid();
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Group deleted successfully" });
    }
}
