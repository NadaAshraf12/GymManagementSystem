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
    protected readonly SignInManager<ApplicationUser>? _signInManager;
    protected ApplicationUser? _currentUser;

    public BaseController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser>? signInManager = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    protected async Task<bool> LoadUserDataAsync()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _currentUser = await _userManager.FindByIdAsync(userId);
                if (_currentUser != null)
                {
                    if (!_currentUser.IsActive)
                    {
                        if (_signInManager != null)
                        {
                            await _signInManager.SignOutAsync();
                        }
                        else
                        {
                            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                        }

                        TempData["Error"] = "Your account has been deactivated. Please contact the administrator.";
                        return false;
                    }

                    ViewBag.ProfilePicture = _currentUser.ProfilePicture;
                    ViewBag.CurrentUser = _currentUser;
                }
            }
        }

        return true;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var isAllowed = LoadUserDataAsync().GetAwaiter().GetResult();

        if (!isAllowed)
        {
            context.Result = RedirectToAction("Login", "Auth");
        }
    }
}
