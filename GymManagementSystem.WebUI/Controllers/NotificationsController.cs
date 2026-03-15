using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationReadDto>>>> Me()
    {
        var notifications = await _notificationService.GetMyNotificationsAsync();
        return ApiOk<IReadOnlyList<NotificationReadDto>>(notifications, "Notifications retrieved successfully.");
    }

    [HttpPost("mark-read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(MarkNotificationReadDto dto)
    {
        await _notificationService.MarkReadAsync(dto);
        return ApiOk<object>(new { }, "Notification marked as read.");
    }
}
