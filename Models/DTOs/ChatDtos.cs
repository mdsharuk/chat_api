namespace chat_dotnet.Models.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public int? ConversationId { get; set; }
    public int? GroupId { get; set; }
    public MessageType MessageType { get; set; }
    public List<MediaDto> Media { get; set; } = new();
}

public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public List<int> MediaIds { get; set; } = new();
}

public class ConversationDto
{
    public int Id { get; set; }
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
}
