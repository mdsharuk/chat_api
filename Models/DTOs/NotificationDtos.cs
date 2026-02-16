namespace chat_dotnet.Models.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? ImageUrl { get; set; }
    public string? FromUserId { get; set; }
    public string? FromUserName { get; set; }
}

public class CreateNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationType Type { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? ImageUrl { get; set; }
    public string? FromUserId { get; set; }
}

public class MarkNotificationReadDto
{
    public int NotificationId { get; set; }
}