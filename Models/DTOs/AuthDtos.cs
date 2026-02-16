using System.ComponentModel.DataAnnotations;

namespace chat_dotnet.Models.DTOs;

public class RegisterDto
{
    [Required]
    [StringLength(50)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public string? ProfilePictureUrl { get; set; }
}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}
