using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class Connection
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string ConnectionId { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
