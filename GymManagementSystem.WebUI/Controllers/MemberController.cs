using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Member,Admin")] 
public class MemberController : BaseController
{
    private readonly IApplicationDbContext _db;
    private readonly ISessionService _sessionService;
    public MemberController(IApplicationDbContext db, ISessionService sessionService, UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _db = db;
        _sessionService = sessionService;
    }

    public async Task<IActionResult> MyPlans()
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var assignment = await _db.TrainerMemberAssignments
            .Include(a => a.Trainer)
            .FirstOrDefaultAsync(a => a.MemberId == memberId);

        var latestTrainingPlan = await _db.TrainingPlans
            .Where(tp => tp.MemberId == memberId)
            .OrderByDescending(tp => tp.CreatedAt)
            .Include(tp => tp.Items)
            .FirstOrDefaultAsync();

        var latestNutritionPlan = await _db.NutritionPlans
            .Where(np => np.MemberId == memberId)
            .OrderByDescending(np => np.CreatedAt)
            .Include(np => np.Items)
            .FirstOrDefaultAsync();

        var upcomingSessions = await _db.MemberSessions
            .Include(ms => ms.WorkoutSession)
            .Where(ms => ms.MemberId == memberId && ms.WorkoutSession.SessionDate >= DateTime.UtcNow.Date)
            .OrderBy(ms => ms.WorkoutSession.SessionDate)
            .Take(10)
            .ToListAsync();

        List<Domain.Entities.WorkoutSession> trainerUpcomingSessions = new();
        if (assignment != null)
        {
            var today = DateTime.UtcNow.Date;
            trainerUpcomingSessions = await _db.WorkoutSessions
                .Where(ws => ws.TrainerId == assignment.TrainerId && ws.SessionDate >= today)
                .OrderBy(ws => ws.SessionDate)
                .ThenBy(ws => ws.StartTime)
                .Take(20)
                .ToListAsync();
        }

        var bookedIds = upcomingSessions.Select(ms => ms.WorkoutSessionId).ToHashSet();

        ViewBag.Assignment = assignment;
        ViewBag.TrainingPlan = latestTrainingPlan;
        ViewBag.NutritionPlan = latestNutritionPlan;
        ViewBag.Upcoming = upcomingSessions;
        ViewBag.TrainerUpcoming = trainerUpcomingSessions;
        ViewBag.BookedSessionIds = bookedIds;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrainingItem(int itemId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var item = await _db.TrainingPlanItems.Include(i => i.TrainingPlan).FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null || item.TrainingPlan.MemberId != memberId)
            return Forbid();

        item.IsCompleted = !item.IsCompleted;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(MyPlans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleNutritionItem(int itemId)
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var item = await _db.NutritionPlanItems.Include(i => i.NutritionPlan).FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null || item.NutritionPlan.MemberId != memberId)
            return Forbid();

        item.IsCompleted = !item.IsCompleted;
        await _db.SaveChangesAsync();
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


