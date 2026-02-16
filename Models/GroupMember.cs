using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class GroupMember
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int GroupId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsAdmin { get; set; }
    
    // Navigation properties
    [ForeignKey("GroupId")]
    public virtual Group Group { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
