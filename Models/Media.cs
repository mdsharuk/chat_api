using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_dotnet.Models;

public class Media
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [Required]
    public MediaType MediaType { get; set; }
    
    public string? ThumbnailPath { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key
    [Required]
    public int UploadedBy { get; set; }
    
    // Navigation properties
    [ForeignKey("UploadedBy")]
    public virtual ApplicationUser Uploader { get; set; } = null!;
    
    public virtual ICollection<MessageMedia> MessageMedia { get; set; } = new List<MessageMedia>();
}

public enum MediaType
{
    Image,
    Video,
    Document
}