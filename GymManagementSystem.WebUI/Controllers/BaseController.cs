using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using GymManagementSystem.Domain.Entities;
using System.Security.Claims;

namespace GymManagementSystem.WebUI.Controllers;

public class BaseController : Controller
{
    protected readonly UserManager<ApplicationUser> _userManager;
    protected ApplicationUser? _currentUser;

    public BaseController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected async Task LoadUserDataAsync()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _currentUser = await _userManager.FindByIdAsync(userId);
                if (_currentUser != null)
                {
                    // Check if user is still active
                    if (!_currentUser.IsActive)
                    {
                        // Sign out the user if they are deactivated
                        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                        TempData["Error"] = "Your account has been deactivated. Please contact the administrator.";
                        return;
                    }
                    
                    ViewBag.ProfilePicture = _currentUser.ProfilePicture;
                    ViewBag.CurrentUser = _currentUser;
                }
            }
        }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        LoadUserDataAsync().GetAwaiter().GetResult();
        
        // If user was deactivated and signed out, redirect to login
        if (User?.Identity?.IsAuthenticated == false && TempData.ContainsKey("Error"))
        {
            context.Result = RedirectToAction("Login", "Auth");
        }
    }
}
