using System.Security.Claims;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Trainer,Admin")]
public class TrainerSessionsController : Controller
{
    private readonly ISessionService _sessionService;
    private readonly IApplicationDbContext _db;
    public TrainerSessionsController(ISessionService sessionService, IApplicationDbContext db)
    {
        _sessionService = sessionService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var sessions = await _sessionService.GetByTrainerAsync(trainerId);
        return View(sessions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateWorkoutSessionDto
        {
            SessionDate = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(10,0,0),
            EndTime = new TimeSpan(11,0,0),
            MaxParticipants = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkoutSessionDto model)
    {
        if (!ModelState.IsValid) return View(model);
        var trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        model.TrainerId = trainerId;
        await _sessionService.CreateAsync(model);
        TempData["Success"] = "Session created";
        return RedirectToAction(nameof(Index));
    }
}


