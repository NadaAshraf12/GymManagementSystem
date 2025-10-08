using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using GymManagementSystem.Domain.Entities;

namespace GymManagementSystem.WebUI.Filters;

public class ProfilePictureFilter : IActionFilter
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfilePictureFilter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
                if (user != null)
                {
                    context.HttpContext.Items["ProfilePicture"] = user.ProfilePicture;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
