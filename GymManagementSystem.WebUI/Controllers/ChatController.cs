using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers
{
    [Authorize(Roles = "Trainer,Member")]
    public class ChatController : BaseController
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService, UserManager<ApplicationUser> userManager)
            : base(userManager)
        {
            _chatService = chatService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var conversations = await _chatService.GetConversationsAsync(userId);

            var model = new ChatIndexViewModel
            {
                CurrentUserId = userId,
                Conversations = conversations.ToList()
            };

            return View(model);
        }
    }
}
