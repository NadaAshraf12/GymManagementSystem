using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IApplicationDbContext _context;
    private readonly ITrainerAssignmentService _assignmentService;

    public AdminController(IApplicationDbContext context, UserManager<ApplicationUser> userManager, ITrainerAssignmentService assignmentService) : base(userManager)
    {
        _context = context;
        _assignmentService = assignmentService;
    }

    // GET: /Admin/LoginAudits
    public async Task<IActionResult> LoginAudits(int page = 1, int pageSize = 20)
    {
        var query = _context.LoginAudits
            .Include(la => la.User)
            .OrderByDescending(la => la.LoginTime);

        var totalCount = await query.CountAsync();
        var loginAudits = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(la => new
            {
                la.Id,
                la.Email,
                UserName = la.User != null ? $"{la.User.FirstName} {la.User.LastName}" : "Unknown",
                la.IpAddress,
                la.LoginTime,
                la.LogoutTime,
                la.IsSuccessful,
                la.FailureReason
            })
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.TotalCount = totalCount;

        return View(loginAudits);
    }

    // GET: /Admin/ActiveSessions
    public async Task<IActionResult> ActiveSessions()
    {
        var activeSessions = await _context.LoginAudits
            .Include(la => la.User)
            .Where(la => la.IsSuccessful && la.LogoutTime == null)
            .OrderByDescending(la => la.LoginTime)
            .Select(la => new
            {
                la.Id,
                la.Email,
                UserName = la.User != null ? $"{la.User.FirstName} {la.User.LastName}" : "Unknown",
                la.IpAddress,
                la.UserAgent,
                la.LoginTime,
                Duration = DateTime.UtcNow - la.LoginTime
            })
            .ToListAsync();

        return View(activeSessions);
    }

    // GET: /Admin/AssignTrainer
    public async Task<IActionResult> AssignTrainer()
    {
        var trainers = await _context.Trainers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync();
        var members = await _context.Members.OrderBy(m => m.FirstName).ThenBy(m => m.LastName).ToListAsync();
        ViewBag.Trainers = trainers;
        ViewBag.Members = members;
        return View();
    }

    // POST: /Admin/AssignTrainer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(string trainerId, string memberId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(trainerId) || string.IsNullOrWhiteSpace(memberId))
        {
            TempData["Error"] = "Please select both trainer and member.";
            return RedirectToAction(nameof(AssignTrainer));
        }
        await _assignmentService.AssignAsync(new Application.DTOs.AssignTrainerDto
        {
            TrainerId = trainerId,
            MemberId = memberId,
            Notes = notes
        });
        TempData["Success"] = "Trainer assigned to member successfully.";
        return RedirectToAction(nameof(AssignTrainer));
    }
}
