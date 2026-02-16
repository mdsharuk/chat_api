using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using chat_dotnet.Data;
using chat_dotnet.Models;
using chat_dotnet.Models.DTOs;
using System.Security.Claims;

namespace chat_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MediaController> _logger;
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB
    private readonly string[] _allowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private readonly string[] _allowedVideoTypes = { "video/mp4", "video/avi", "video/mov", "video/wmv" };

    public MediaController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<MediaController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia([FromForm] UploadMediaDto uploadDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest("No file provided");

            if (uploadDto.File.Length > _maxFileSize)
                return BadRequest("File size exceeds maximum allowed size");

            // Validate file type
            var allowedTypes = uploadDto.MediaType == MediaType.Image ? _allowedImageTypes : _allowedVideoTypes;
            if (!allowedTypes.Contains(uploadDto.File.ContentType.ToLower()))
                return BadRequest("Invalid file type");

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(uploadDto.File.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await uploadDto.File.CopyToAsync(fileStream);
            }

            // Generate thumbnail for images
            string? thumbnailPath = null;
            if (uploadDto.MediaType == MediaType.Image)
            {
                thumbnailPath = await GenerateThumbnail(filePath, fileName);
            }

            // Save to database
            var media = new Media
            {
                FileName = uploadDto.File.FileName,
                FilePath = $"/uploads/{fileName}",
                ContentType = uploadDto.File.ContentType,
                FileSize = uploadDto.File.Length,
                MediaType = uploadDto.MediaType,
                ThumbnailPath = thumbnailPath,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Media.Add(media);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var response = new MediaUploadResponseDto
            {
                Success = true,
                Message = "File uploaded successfully",
                Media = new MediaDto
                {
                    Id = media.Id,
                    FileName = media.FileName,
                    FilePath = media.FilePath,
                    ContentType = media.ContentType,
                    FileSize = media.FileSize,
                    MediaType = media.MediaType,
                    ThumbnailPath = media.ThumbnailPath,
                    UploadedAt = media.UploadedAt,
                    UploadedBy = userId,
                    UploaderName = user?.UserName ?? "Unknown"
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media");
            return StatusCode(500, new MediaUploadResponseDto 
            { 
                Success = false, 
                Message = "An error occurred while uploading the file" 
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedia(int id)
    {
        var media = await _context.Media
            .Include(m => m.Uploader)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (media == null)
            return NotFound();

        var mediaDto = new MediaDto
        {
            Id = media.Id,
            FileName = media.FileName,
            FilePath = media.FilePath,
            ContentType = media.ContentType,
            FileSize = media.FileSize,
            MediaType = media.MediaType,
            ThumbnailPath = media.ThumbnailPath,
            UploadedAt = media.UploadedAt,
            UploadedBy = media.UploadedBy,
            UploaderName = media.Uploader.UserName ?? "Unknown"
        };

        return Ok(mediaDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedia(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var media = await _context.Media.FindAsync(id);

        if (media == null)
            return NotFound();

        // Only allow delete if user owns the media or is admin
        if (media.UploadedBy != userId && !User.IsInRole("Admin"))
            return Forbid();

        try
        {
            // Delete physical file
            var fullPath = Path.Combine(_environment.WebRootPath, media.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(media.ThumbnailPath))
            {
                var thumbnailPath = Path.Combine(_environment.WebRootPath, media.ThumbnailPath.TrimStart('/'));
                if (System.IO.File.Exists(thumbnailPath))
                    System.IO.File.Delete(thumbnailPath);
            }

            // Delete from database
            _context.Media.Remove(media);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Media deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media");
            return StatusCode(500, "An error occurred while deleting the media");
        }
    }

    private async Task<string?> GenerateThumbnail(string originalPath, string originalFileName)
    {
        try
        {
            // This is a basic implementation - in production, you'd want to use a proper image library
            // like ImageSharp or SkiaSharp to generate proper thumbnails
            var thumbnailsPath = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
            if (!Directory.Exists(thumbnailsPath))
                Directory.CreateDirectory(thumbnailsPath);

            var thumbnailFileName = $"thumb_{originalFileName}";
            var thumbnailPath = Path.Combine(thumbnailsPath, thumbnailFileName);

            // For now, just copy the original file as thumbnail
            // In production, you'd resize the image here
            System.IO.File.Copy(originalPath, thumbnailPath, true);

            return $"/uploads/thumbnails/{thumbnailFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail");
            return null;
        }
    }
}