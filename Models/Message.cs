using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class Message
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Reply support
    public int? ReplyToMessageId { get; set; }
    
    // Foreign keys
    [Required]
    public int SenderId { get; set; }
    
    public int? ConversationId { get; set; }
    
    public int? GroupId { get; set; }
    
    // Message type
    public MessageType MessageType { get; set; } = MessageType.Text;
    
    // Navigation properties
    [ForeignKey("SenderId")]
    public virtual ApplicationUser Sender { get; set; } = null!;
    
    [ForeignKey("ConversationId")]
    public virtual Conversation? Conversation { get; set; }
    
    [ForeignKey("GroupId")]
    public virtual Group? Group { get; set; }
    
    [ForeignKey("ReplyToMessageId")]
    public virtual Message? ReplyToMessage { get; set; }
    
    public virtual ICollection<MessageMedia> MessageMedia { get; set; } = new List<MessageMedia>();
}

public enum MessageType
{
    Text,
    Media,
    System
}
