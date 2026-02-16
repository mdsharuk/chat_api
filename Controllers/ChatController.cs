using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Data;
using chat_dotnet.Models.DTOs;

namespace chat_dotnet.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ApplicationDbContext context, ILogger<ChatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var currentUserIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserIdValue == null || !int.TryParse(currentUserIdValue, out var currentUserId)) return Unauthorized();
        
        var users = await _context.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                FullName = u.FullName,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Bio = u.Bio
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var conversations = await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                OtherUserId = c.User1Id == userId ? c.User2Id : c.User1Id,
                OtherUserName = c.User1Id == userId ? (c.User2.UserName ?? "") : (c.User1.UserName ?? ""),
                IsOnline = c.User1Id == userId ? c.User2.IsOnline : c.User1.IsOnline,
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault() != null 
                    ? c.Messages.OrderByDescending(m => m.SentAt).First().Content 
                    : null,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("conversation/{otherUserId}")]
    public async Task<IActionResult> GetConversationMessages(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == userId && c.User2Id == otherUserId) ||
                (c.User1Id == otherUserId && c.User2Id == userId));

        if (conversation == null)
        {
            return Ok(new List<MessageDto>());
        }

        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .Include(m => m.Sender)
            .Include(m => m.MessageMedia)
                .ThenInclude(mm => mm.Media)
                    .ThenInclude(media => media.Uploader)
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
                IsRead = m.IsRead,
                ConversationId = m.ConversationId,
                MessageType = m.MessageType,
                Media = m.MessageMedia.Select(mm => new MediaDto
                {
                    Id = mm.Media.Id,
                    FileName = mm.Media.FileName,
                    FilePath = mm.Media.FilePath,
                    ContentType = mm.Media.ContentType,
                    FileSize = mm.Media.FileSize,
                    MediaType = mm.Media.MediaType,
                    ThumbnailPath = mm.Media.ThumbnailPath,
                    UploadedAt = mm.Media.UploadedAt,
                    UploadedBy = mm.Media.UploadedBy,
                    UploaderName = mm.Media.Uploader.UserName ?? "Unknown"
                }).ToList()
            })
            .ToListAsync();

        messages.Reverse();

        return Ok(messages);
    }

    [HttpPost("mark-read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        var message = await _context.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return NotFound();

        // Verify user is participant
        if (message.Conversation != null)
        {
            if (message.Conversation.User1Id != userId && message.Conversation.User2Id != userId)
            {
                return Forbid();
            }

            if (message.SenderId != userId && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Ok();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        var currentUserIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserIdValue == null || !int.TryParse(currentUserIdValue, out var currentUserId)) return Unauthorized();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query cannot be empty");
        }

        var users = await _context.Users
            .Where(u => u.Id != currentUserId && 
                       (u.UserName!.Contains(query) || 
                        (u.FullName != null && u.FullName.Contains(query))))
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                FullName = u.FullName,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Bio = u.Bio
            })
            .Take(20)
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("search-messages")]
    public async Task<IActionResult> SearchMessages([FromBody] SearchMessageDto searchDto)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId)) return Unauthorized();

        // Build base query
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Conversation)
            .Include(m => m.Group)
            .Include(m => m.MessageMedia)
                .ThenInclude(mm => mm.Media)
                    .ThenInclude(media => media.Uploader)
            .AsQueryable();

        // Filter by user access (only messages from conversations/groups user is part of)
        query = query.Where(m => 
            (m.ConversationId != null && 
             (m.Conversation!.User1Id == userId || m.Conversation!.User2Id == userId)) ||
            (m.GroupId != null && 
             m.Group!.Members.Any(gm => gm.UserId == userId)));

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            query = query.Where(m => m.Content.Contains(searchDto.SearchTerm));
        }

        if (searchDto.ConversationId.HasValue)
        {
            query = query.Where(m => m.ConversationId == searchDto.ConversationId.Value);
        }

        if (searchDto.GroupId.HasValue)
        {
            query = query.Where(m => m.GroupId == searchDto.GroupId.Value);
        }

        if (searchDto.SenderId.HasValue)
        {
            query = query.Where(m => m.SenderId == searchDto.SenderId.Value);
        }

        if (searchDto.FromDate.HasValue)
        {
            query = query.Where(m => m.SentAt >= searchDto.FromDate.Value);
        }

        if (searchDto.ToDate.HasValue)
        {
            query = query.Where(m => m.SentAt <= searchDto.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName ?? string.Empty,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                ConversationId = m.ConversationId,
                GroupId = m.GroupId,
                MessageType = m.MessageType,
                Media = m.MessageMedia.Select(mm => new MediaDto
                {
                    Id = mm.Media.Id,
                    FileName = mm.Media.FileName,
                    FilePath = mm.Media.FilePath,
                    ContentType = mm.Media.ContentType,
                    FileSize = mm.Media.FileSize,
                    MediaType = mm.Media.MediaType,
                    ThumbnailPath = mm.Media.ThumbnailPath,
                    UploadedAt = mm.Media.UploadedAt,
                    UploadedBy = mm.Media.UploadedBy,
                    UploaderName = mm.Media.Uploader.UserName ?? "Unknown"
                }).ToList()
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)searchDto.PageSize);

        var result = new SearchMessageResultDto
        {
            Messages = messages,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = totalPages
        };

        return Ok(result);
    }
}
