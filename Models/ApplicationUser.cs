using Microsoft.AspNetCore.Identity;

namespace chat_dotnet.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    
    // Navigation properties
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public virtual ICollection<Media> UploadedMedia { get; set; } = new List<Media>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
}
