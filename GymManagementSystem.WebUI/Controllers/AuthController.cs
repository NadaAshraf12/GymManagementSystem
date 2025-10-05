using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagementSystem.WebUI.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _authService = authService;
    }

    // GET: /Auth/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Auth/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        if (!ModelState.IsValid) return View(loginDto);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var result = await _authService.LoginAsync(loginDto, ipAddress, userAgent);

            // Store tokens in session (server)
            HttpContext.Session.SetString("AccessToken", result.AccessToken ?? string.Empty);
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken ?? string.Empty);

            // Also pass tokens once via TempData so client-side AuthTest can pick them up
            if (!string.IsNullOrEmpty(result.AccessToken))
                TempData["AccessToken"] = result.AccessToken;
            if (!string.IsNullOrEmpty(result.RefreshToken))
                TempData["RefreshToken"] = result.RefreshToken;

            // Create cookie-based authentication principal so MVC views see User.Identity.IsAuthenticated
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId),
            new Claim(ClaimTypes.Name, result.FullName ?? result.Email),
            new Claim(ClaimTypes.Email, result.Email)
        };

            if (!string.IsNullOrEmpty(result.Role))
                claims.Add(new Claim(ClaimTypes.Role, result.Role));

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

            TempData["Success"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            // for debugging show detailed message (in prod you might log and show friendly text)
            ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
        }

        return View(loginDto);
    }

    // GET: /Auth/Register
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Auth/Register
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        if (!ModelState.IsValid) return View(registerDto);

        try
        {
            var result = await _authService.RegisterAsync(registerDto);

            // If RegisterAsync throws exception it will be caught below.
            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction("Login", "Auth");
        }
        catch (ArgumentException ex)
        {
            // Identity errors usually thrown as ArgumentException in your service — show message
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            // show real error for debugging
            ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
        }

        return View(registerDto);
    }
    // GET: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _authService.LogoutAsync(userId);
        }

        HttpContext.Session.Remove("AccessToken");
        HttpContext.Session.Remove("RefreshToken");
        
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        
        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Auth/ChangePassword
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // POST: /Auth/ChangePassword
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _authService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

                if (result)
                {
                    TempData["Success"] = "Password changed successfully!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to change password.");
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(changePasswordDto);
    }

    // POST: /Auth/RefreshToken
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ipAddress, userAgent);

            // Update session with new tokens
            HttpContext.Session.SetString("AccessToken", result.AccessToken);
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: /Auth/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}