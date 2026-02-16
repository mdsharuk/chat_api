using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using chat_dotnet.Models;
using chat_dotnet.Models.DTOs;
using chat_dotnet.Services;
using System.Security.Claims;

namespace chat_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            CreatedAt = DateTime.UtcNow,
            IsOnline = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Assign default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        _logger.LogInformation($"New user registered: {user.UserName}");

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation($"User logged in: {user.UserName}");

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            UserId = user.Id,
            Roles = roles.ToList(),
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logout successful" });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.IsOnline,
            user.LastSeen,
            user.ProfilePictureUrl,
            user.Bio,
            Roles = roles
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto model)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue == null || !int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Update text fields
            if (!string.IsNullOrWhiteSpace(model.FullName))
                user.FullName = model.FullName;

            if (model.Bio != null) // Allow empty string to clear bio
                user.Bio = model.Bio;

            // Handle profile picture upload
            if (model.ProfilePicture != null)
            {
                // Validate file
                if (model.ProfilePicture.Length > 5 * 1024 * 1024) // 5MB limit
                    return BadRequest("Profile picture size cannot exceed 5MB");

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(model.ProfilePicture.ContentType.ToLower()))
                    return BadRequest("Only JPEG, PNG, GIF and WebP images are allowed");

                // Create uploads directory
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Delete old profile picture
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // Save new profile picture
                var fileExtension = Path.GetExtension(model.ProfilePicture.FileName);
                var fileName = $"profile_{userId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            _logger.LogInformation($"Profile updated for user: {user.UserName}");

            return Ok(new
            {
                message = "Profile updated successfully",
                user = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    user.ProfilePictureUrl,
                    user.Bio
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, "An error occurred while updating the profile");
        }
    }
}
