using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class MessageMedia
{
    [Key]
    public int Id { get; set; }
    
    // Foreign keys
    public int MessageId { get; set; }
    public int MediaId { get; set; }
    
    // Navigation properties
    [ForeignKey("MessageId")]
    public virtual Message Message { get; set; } = null!;
    
    [ForeignKey("MediaId")]
    public virtual Media Media { get; set; } = null!;
}