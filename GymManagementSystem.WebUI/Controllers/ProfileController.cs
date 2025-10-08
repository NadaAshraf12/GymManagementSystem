using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class ProfileController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(
        UserManager<ApplicationUser> userManager, 
        IAuthService authService,
        IWebHostEnvironment environment) : base(userManager)
    {
        _authService = authService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var profileViewModel = new ProfileViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? "Member"
        };

        return View(profileViewModel);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var editViewModel = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture
        };

        return View(editViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;

        if (user.Email != model.Email)
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null && emailExists.Id != userId)
            {
                ModelState.AddModelError("Email", "Email is already taken by another user.");
                return View(model);
            }

            user.Email = model.Email;
            user.UserName = model.Email; 
        }

        if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
        {
            var fileName = await SaveProfileImageAsync(model.ProfileImageFile, userId);
            if (!string.IsNullOrEmpty(fileName))
            {
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    DeleteProfileImage(user.ProfilePicture);
                }
                user.ProfilePicture = fileName;
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Profile updated successfully!";
            
            if (user.Email != model.Email)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any())
                    claims.Add(new Claim(ClaimTypes.Role, roles.First()));

                var identity = new ClaimsIdentity(claims, "Application");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Application", principal);
            }

            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePicture()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            DeleteProfileImage(user.ProfilePicture);
            user.ProfilePicture = null;
            await _userManager.UpdateAsync(user);
        }

        TempData["Success"] = "Profile picture deleted successfully!";
        return RedirectToAction("Index");
    }

    private async Task<string> SaveProfileImageAsync(IFormFile file, string userId)
    {
        try
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(fileExtension))
            {
                return string.Empty;
            }

            var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void DeleteProfileImage(string fileName)
    {
        try
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch
        {
        }
    }
}
