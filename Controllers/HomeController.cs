using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace chat_dotnet.Controllers;

/// <summary>
/// Controller for rendering Razor views and managing client-side navigation
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Landing page - redirects to login if not authenticated, otherwise to chat
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Chat");
        }
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Login page view
    /// </summary>
    [HttpGet]
    public IActionResult Login()
    {
        // If already authenticated, redirect to chat
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Chat");
        }
        return View();
    }

    /// <summary>
    /// Registration page view
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        // If already authenticated, redirect to chat
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Chat");
        }
        return View();
    }

    /// <summary>
    /// Main chat interface - requires authentication (checked client-side)
    /// </summary>
    [HttpGet]
    public IActionResult Chat()
    {
        // Note: Authentication is handled client-side with JWT tokens
        // The client will redirect to login if no valid token exists
        return View();
    }

    /// <summary>
    /// Logout action
    /// </summary>
    [HttpPost]
    public IActionResult Logout()
    {
        // Client-side will handle JWT token removal
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Error page
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
