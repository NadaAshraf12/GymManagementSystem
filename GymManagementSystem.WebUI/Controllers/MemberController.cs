using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Member,Admin")] 
public class MemberController : BaseController
{
    private readonly ISessionService _sessionService;
    private readonly IMemberPlansService _memberPlansService;

    public MemberController(
        ISessionService sessionService,
        IMemberPlansService memberPlansService,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _sessionService = sessionService;
        _memberPlansService = memberPlansService;
    }

    public async Task<IActionResult> MyPlans()
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var snapshot = await _memberPlansService.GetSnapshotAsync(memberId);

        ViewBag.Assignment = snapshot.Assignment;
        ViewBag.TrainingPlan = snapshot.TrainingPlan;
        ViewBag.NutritionPlan = snapshot.NutritionPlan;
        ViewBag.Upcoming = snapshot.UpcomingBookings;
        ViewBag.TrainerUpcoming = snapshot.TrainerUpcomingSessions;
        ViewBag.BookedSessionIds = snapshot.BookedSessionIds;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrainingItem(int itemId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var ok = await _memberPlansService.ToggleTrainingItemAsync(memberId, itemId);
        if (!ok)
            return Forbid();
        return RedirectToAction(nameof(MyPlans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleNutritionItem(int itemId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var ok = await _memberPlansService.ToggleNutritionItemAsync(memberId, itemId);
        if (!ok)
            return Forbid();
        return RedirectToAction(nameof(MyPlans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttendSession(int workoutSessionId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(memberId)) return Unauthorized();

        var ok = await _sessionService.BookMemberAsync(new Application.DTOs.BookMemberToSessionDto
        {
            MemberId = memberId,
            WorkoutSessionId = workoutSessionId
        });
        if (!ok) TempData["Error"] = "Cannot book session (full or invalid).";
        else TempData["Success"] = "You are booked for this session.";
        return RedirectToAction(nameof(MyPlans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotAttendSession(int workoutSessionId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(memberId)) return Unauthorized();

        var ok = await _sessionService.CancelBookingAsync(memberId, workoutSessionId);
        if (!ok) TempData["Error"] = "Cannot cancel booking.";
        else TempData["Success"] = "Your booking was cancelled.";
        return RedirectToAction(nameof(MyPlans));
    }
}


