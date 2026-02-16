namespace chat_dotnet.Models.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
    public int? ConversationId { get; set; }
    public int? GroupId { get; set; }
    public MessageType MessageType { get; set; }
    public List<MediaDto> Media { get; set; } = new();
    public int? ReplyToMessageId { get; set; }
    public ReplyMessageDto? ReplyToMessage { get; set; }
}

public class ReplyMessageDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
}

public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public List<int> MediaIds { get; set; } = new();
}

public class ConversationDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
}
public class SearchMessageDto
{
    public string? SearchTerm { get; set; }
    public int? ConversationId { get; set; }
    public int? GroupId { get; set; }
    public int? SenderId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SearchMessageResultDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}