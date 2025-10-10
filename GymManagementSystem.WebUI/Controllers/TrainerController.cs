using System.Security.Claims;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Trainer,Admin")]
public class TrainerController : Controller
{
    private readonly ITrainerAssignmentService _assignmentService;
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly INutritionPlanService _nutritionPlanService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISessionService _sessionService;

    public TrainerController(
        ITrainerAssignmentService assignmentService,
        ITrainingPlanService trainingPlanService,
        INutritionPlanService nutritionPlanService,
        IUnitOfWork unitOfWork,
        ISessionService sessionService)
    {
        _assignmentService = assignmentService;
        _trainingPlanService = trainingPlanService;
        _nutritionPlanService = nutritionPlanService;
        _unitOfWork = unitOfWork;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> MyMembers()
    {
        var trainerId = GetCurrentTrainerId();
        var assignments = await _assignmentService.GetAssignmentsForTrainerAsync(trainerId);

        var memberIds = assignments.Select(a => a.MemberId).ToList();
        var membersRepo = _unitOfWork.Repository<Member>();
        var tpItemRepo = _unitOfWork.Repository<TrainingPlanItem>();
        var npItemRepo = _unitOfWork.Repository<NutritionPlanItem>();

        var members = await membersRepo.Query().Where(m => memberIds.Contains(m.Id))
            .Select(m => new TrainerMemberListItem
            {
                MemberId = m.Id,
                Name = m.FirstName + " " + m.LastName,
                MemberCode = m.MemberCode,
                TrainingCompleted = tpItemRepo.Query()
                    .Where(i => i.TrainingPlan.MemberId == m.Id && i.IsCompleted)
                    .Count(),
                TrainingTotal = tpItemRepo.Query()
                    .Count(i => i.TrainingPlan.MemberId == m.Id),
                NutritionCompleted = npItemRepo.Query()
                    .Where(i => i.NutritionPlan.MemberId == m.Id && i.IsCompleted)
                    .Count(),
                NutritionTotal = npItemRepo.Query()
                    .Count(i => i.NutritionPlan.MemberId == m.Id)
            }).ToListAsync();

        return View(members);
    }

    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var trainerId = GetCurrentTrainerId();
        var today = DateTime.UtcNow.Date;
        var wsRepo = _unitOfWork.Repository<WorkoutSession>();
        var sessions = await wsRepo.Query()
            .Where(ws => ws.TrainerId == trainerId && ws.SessionDate >= today)
            .OrderBy(ws => ws.SessionDate).ThenBy(ws => ws.StartTime)
            .Include(ws => ws.MemberSessions)
            .ToListAsync();
        return View(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> SessionAttendance(int id)
    {
        var trainerId = GetCurrentTrainerId();
        var wsRepo = _unitOfWork.Repository<WorkoutSession>();
        var session = await wsRepo.Query()
            .Include(ws => ws.MemberSessions)
            .ThenInclude(ms => ms.Member)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.TrainerId == trainerId);
        if (session == null) return NotFound();
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttendance(int memberSessionId, bool attended)
    {
        var msRepo = _unitOfWork.Repository<MemberSession>();
        var ms = await msRepo.Query().Include(x => x.WorkoutSession).FirstOrDefaultAsync(x => x.Id == memberSessionId);
        if (ms == null) return NotFound();
        var trainerId = GetCurrentTrainerId();
        if (ms.WorkoutSession.TrainerId != trainerId) return Forbid();
        ms.Attended = attended;
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Attendance updated.";
        return RedirectToAction(nameof(SessionAttendance), new { id = ms.WorkoutSessionId });
    }

    [HttpGet]
    public IActionResult CreateTrainingPlan(string memberId)
    {
        var model = new CreateTrainingPlanViewModel
        {
            MemberId = memberId,
            Items = new List<CreateTrainingPlanItemViewModel>()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrainingPlan(CreateTrainingPlanViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var trainerId = GetCurrentTrainerId();
        var dto = new CreateTrainingPlanDto
        {
            MemberId = model.MemberId,
            TrainerId = trainerId,
            Title = model.Title,
            Notes = model.Notes,
            Items = model.Items.Select(i => new CreateTrainingPlanItemDto
            {
                DayOfWeek = i.DayOfWeek,
                ExerciseName = i.ExerciseName,
                Sets = i.Sets,
                Reps = i.Reps,
                Notes = i.Notes
            }).ToList()
        };
        await _trainingPlanService.CreateAsync(dto);
        TempData["Success"] = "Training plan created";
        return RedirectToAction(nameof(MyMembers));
    }

    [HttpGet]
    public IActionResult CreateNutritionPlan(string memberId)
    {
        var model = new CreateNutritionPlanViewModel
        {
            MemberId = memberId,
            Items = new List<CreateNutritionPlanItemViewModel>()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNutritionPlan(CreateNutritionPlanViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var trainerId = GetCurrentTrainerId();
        var dto = new CreateNutritionPlanDto
        {
            MemberId = model.MemberId,
            TrainerId = trainerId,
            Title = model.Title,
            Notes = model.Notes,
            Items = model.Items.Select(i => new CreateNutritionPlanItemDto
            {
                DayOfWeek = i.DayOfWeek,
                MealType = i.MealType,
                FoodDescription = i.FoodDescription,
                Calories = i.Calories,
                Notes = i.Notes
            }).ToList()
        };
        await _nutritionPlanService.CreateAsync(dto);
        TempData["Success"] = "Nutrition plan created";
        return RedirectToAction(nameof(MyMembers));
    }

    private string GetCurrentTrainerId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id ?? string.Empty;
    }
}


