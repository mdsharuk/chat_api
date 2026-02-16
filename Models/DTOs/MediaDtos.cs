namespace chat_dotnet.Models.DTOs;

public class MediaDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public MediaType MediaType { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedBy { get; set; }
    public string UploaderName { get; set; } = string.Empty;
}

public class UploadMediaDto
{
    public IFormFile File { get; set; } = null!;
    public MediaType MediaType { get; set; }
}

public class MediaUploadResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public MediaDto? Media { get; set; }
}