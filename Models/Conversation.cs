using System.ComponentModel.DataAnnotations;

namespace chat_dotnet.Models;

public class Conversation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int User1Id { get; set; }
    
    [Required]
    public int User2Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastMessageAt { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser User1 { get; set; } = null!;
    public virtual ApplicationUser User2 { get; set; } = null!;
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
