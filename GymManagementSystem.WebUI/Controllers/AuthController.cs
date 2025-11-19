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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

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

            if (!string.IsNullOrEmpty(result.AccessToken))
                TempData["AccessToken"] = result.AccessToken;
            if (!string.IsNullOrEmpty(result.RefreshToken))
                TempData["RefreshToken"] = result.RefreshToken;

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

            if (result.MustChangePassword)
            {
                TempData["Info"] = "Please change your password to continue.";
                return RedirectToAction("ChangePassword", "Auth");
            }

            TempData["Success"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Check if it's an account deactivation error
            if (ex.Message.Contains("deactivated"))
            {
                TempData["Error"] = ex.Message;
            }
            else
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
        }

        return View(loginDto);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        if (!ModelState.IsValid) return View(registerDto);

        try
        {
            var result = await _authService.RegisterAsync(registerDto);

            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction("Login", "Auth");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
        }

        return View(registerDto);
    }
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

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

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

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ipAddress, userAgent);

            HttpContext.Session.SetString("AccessToken", result.AccessToken);
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { 
                message = ex.Message,
                isAccountDeactivated = ex.Message.Contains("deactivated")
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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