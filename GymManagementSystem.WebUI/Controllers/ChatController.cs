using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GymManagementSystem.WebUI.Controllers
{
    [Authorize(Roles = "Trainer,Member")]
    public class ChatController : BaseController
    {
        private readonly IChatService _chatService;
        private readonly IWebHostEnvironment _env;

        public ChatController(IChatService chatService, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
            : base(userManager)
        {
            _chatService = chatService;
            _env = env;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<ApiResponse<ChatUploadResultDto>>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                var response = ApiResponse<ChatUploadResultDto>.Fail(
                    "No file uploaded.",
                    StatusCodes.Status400BadRequest);
                return BadRequest(response);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                var response = ApiResponse<ChatUploadResultDto>.Fail(
                    "Unsupported file type.",
                    StatusCodes.Status400BadRequest);
                return BadRequest(response);
            }

            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
            {
                var response = ApiResponse<ChatUploadResultDto>.Fail(
                    "File too large.",
                    StatusCodes.Status400BadRequest);
                return BadRequest(response);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "chat_uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, safeName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/chat_uploads/{safeName}";
            var result = new ChatUploadResultDto { Url = url };
            var ok = ApiResponse<ChatUploadResultDto>.Ok(
                result,
                "Upload successful.",
                StatusCodes.Status200OK);
            return Ok(ok);
        }
    }
}
