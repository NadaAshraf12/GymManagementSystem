using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace GymManagementSystem.WebUI.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private SignInManager<ApplicationUser> SignInManager =>
        _signInManager ?? throw new InvalidOperationException("SignInManager is not configured.");

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : base(userManager, signInManager)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto loginDto, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid) return View(loginDto);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var result = await _authService.LoginAsync(loginDto, ipAddress, userAgent);

            await SignInWithTokensAsync(result);

            if (result.MustChangePassword)
            {
                TempData["Info"] = "Please change your password to continue.";
                return RedirectToAction(nameof(ChangePassword));
            }

            TempData["Success"] = "Login successful!";
            return LocalRedirect(returnUrl);
        }
        catch (UnauthorizedAccessException ex)
        {
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

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            TempData["Error"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await SignInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            var fallbackResult = await HandleExternalLoginWithoutInfoAsync(returnUrl);
            if (fallbackResult != null)
            {
                return fallbackResult;
            }

            TempData["Error"] = "Error loading external login information.";
            return RedirectToAction(nameof(Login));
        }

        var signInResult = await SignInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            await SignInManager.UpdateExternalAuthenticationTokensAsync(info);

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await SignInWithTokensAsync(new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Role = roles.FirstOrDefault() ?? "Member"
                });
            }

            TempData["Success"] = "Login successful!";
            return LocalRedirect(returnUrl);
        }

        if (signInResult.IsLockedOut)
        {
            TempData["Error"] = "Your account is locked out.";
            return RedirectToAction(nameof(Login));
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ProviderDisplayName"] = info.ProviderDisplayName;

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "The external provider did not return an email address.";
            return RedirectToAction(nameof(Login));
        }

        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName)
                        ?? info.Principal.FindFirstValue(ClaimTypes.Name)
                        ?? string.Empty;
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

        var model = new ExternalLoginConfirmationViewModel
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        TempData["GoogleProviderKey"] = info.ProviderKey;

        return View("ExternalLoginConfirmation", model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await SignInManager.GetExternalLoginInfoAsync();
        UserLoginInfo? loginInfo = null;

        if (info == null)
        {
            var providerKey = TempData["GoogleProviderKey"]?.ToString();
            if (!string.IsNullOrWhiteSpace(providerKey))
            {
                loginInfo = new UserLoginInfo("Google", providerKey, "Google");
            }
            else
            {
                var authResult = await HttpContext.AuthenticateAsync("Google");
                if (authResult?.Succeeded == true && authResult.Principal != null)
                {
                    var claims = authResult.Principal.Claims.ToList();
                    var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                    var providerKeyClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrWhiteSpace(providerKeyClaim) && emailClaim == model.Email)
                    {
                        loginInfo = new UserLoginInfo("Google", providerKeyClaim, "Google");
                    }
                }
            }

            if (loginInfo == null)
            {
                TempData["Error"] = "Error loading external login information during confirmation. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }
        else
        {
            loginInfo = new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.ProviderDisplayName);
        }

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, loginInfo);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");

                    var authResult = new AuthResponseDto
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Role = "Member"
                    };

                    await SignInWithTokensAsync(authResult);

                    TempData["Success"] = "Account created successfully!";
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
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
            await _authService.RegisterAsync(registerDto);

            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _authService.LogoutAsync(userId);
        }

        HttpContext.Session.Remove("AccessToken");
        HttpContext.Session.Remove("RefreshToken");

        await SignInManager.SignOutAsync();

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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

                ModelState.AddModelError(string.Empty, "Failed to change password.");
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
            return Unauthorized(new
            {
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

    private async Task<IActionResult?> HandleExternalLoginWithoutInfoAsync(string returnUrl)
    {
        var authResult = await HttpContext.AuthenticateAsync("Google");
        if (authResult?.Succeeded != true || authResult.Principal == null)
        {
            return null;
        }

        var claims = authResult.Principal.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var givenName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(providerKey))
        {
            return null;
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var externalLogins = await _userManager.GetLoginsAsync(existingUser);
            var googleLogin = externalLogins.FirstOrDefault(l => l.LoginProvider == "Google");

            if (googleLogin != null)
            {
                await SignInManager.SignInAsync(existingUser, isPersistent: false);

                var roles = await _userManager.GetRolesAsync(existingUser);
                await SignInWithTokensAsync(new AuthResponseDto
                {
                    UserId = existingUser.Id,
                    Email = existingUser.Email ?? string.Empty,
                    FullName = $"{existingUser.FirstName} {existingUser.LastName}",
                    Role = roles.FirstOrDefault() ?? "Member"
                });

                TempData["Success"] = "Login successful!";
                return LocalRedirect(returnUrl);
            }

            var loginInfo = new UserLoginInfo("Google", providerKey, "Google");
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, loginInfo);
            if (addLoginResult.Succeeded)
            {
                await SignInManager.SignInAsync(existingUser, isPersistent: false);
                TempData["Success"] = "Login successful!";
                return LocalRedirect(returnUrl);
            }

            foreach (var error in addLoginResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return RedirectToAction(nameof(Login));
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ProviderDisplayName"] = "Google";

        var model = new ExternalLoginConfirmationViewModel
        {
            Email = email,
            FirstName = givenName ?? email,
            LastName = surname ?? string.Empty
        };

        TempData["GoogleProviderKey"] = providerKey;
        return View("ExternalLoginConfirmation", model);
    }

    private async Task SignInWithTokensAsync(AuthResponseDto result)
    {
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
    }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ExternalLoginConfirmationViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
}