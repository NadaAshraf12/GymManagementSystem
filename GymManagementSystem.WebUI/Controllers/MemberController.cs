using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Member,Admin")] 
public class MemberController : Controller
{
    private readonly IApplicationDbContext _db;
    public MemberController(IApplicationDbContext db)
    {
        _db = db;
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

        // Upcoming sessions created by assigned trainer (not yet booked by member)
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

        ViewBag.Assignment = assignment;
        ViewBag.TrainingPlan = latestTrainingPlan;
        ViewBag.NutritionPlan = latestNutritionPlan;
        ViewBag.Upcoming = upcomingSessions;
        ViewBag.TrainerUpcoming = trainerUpcomingSessions;

        return View();
    }
}


