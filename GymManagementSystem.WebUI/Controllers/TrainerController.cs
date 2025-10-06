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
    private readonly IApplicationDbContext _db;

    public TrainerController(
        ITrainerAssignmentService assignmentService,
        ITrainingPlanService trainingPlanService,
        INutritionPlanService nutritionPlanService,
        IApplicationDbContext db)
    {
        _assignmentService = assignmentService;
        _trainingPlanService = trainingPlanService;
        _nutritionPlanService = nutritionPlanService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> MyMembers()
    {
        var trainerId = GetCurrentTrainerId();
        var assignments = await _assignmentService.GetAssignmentsForTrainerAsync(trainerId);

        var memberIds = assignments.Select(a => a.MemberId).ToList();
        var members = await _db.Members.Where(m => memberIds.Contains(m.Id))
            .Select(m => new TrainerMemberListItem
            {
                MemberId = m.Id,
                Name = m.FirstName + " " + m.LastName,
                MemberCode = m.MemberCode
            }).ToListAsync();

        return View(members);
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
        // For Admin acting as trainer, allow query param fallback
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id ?? string.Empty;
    }
}


