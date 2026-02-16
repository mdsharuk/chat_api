using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    // Additional data for different notification types
    public string? RelatedEntityId { get; set; }
    
    public string? ImageUrl { get; set; }
    
    // Foreign keys
    [Required]
    public int UserId { get; set; }
    
    public int? FromUserId { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
    
    [ForeignKey("FromUserId")]
    public virtual ApplicationUser? FromUser { get; set; }
}

public enum NotificationType
{
    NewMessage,
    NewGroupMessage,
    GroupInvite,
    UserOnline,
    MessageReaction,
    MediaUpload,
    ProfileUpdate
}