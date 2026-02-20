using System.Security.Claims;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Trainer,Admin")]
public class TrainerController : Controller
{
    private readonly ITrainerAssignmentService _assignmentService;
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly INutritionPlanService _nutritionPlanService;
    private readonly ITrainerDashboardService _trainerDashboardService;
    private readonly ISessionService _sessionService;
    private readonly ICommissionService _commissionService;
    private readonly ITrainerService _trainerService;

    public TrainerController(
        ITrainerAssignmentService assignmentService,
        ITrainingPlanService trainingPlanService,
        INutritionPlanService nutritionPlanService,
        ITrainerDashboardService trainerDashboardService,
        ISessionService sessionService,
        ICommissionService commissionService,
        ITrainerService trainerService)
    {
        _assignmentService = assignmentService;
        _trainingPlanService = trainingPlanService;
        _nutritionPlanService = nutritionPlanService;
        _trainerDashboardService = trainerDashboardService;
        _sessionService = sessionService;
        _commissionService = commissionService;
        _trainerService = trainerService;
    }

    [HttpGet]
    public async Task<IActionResult> MyMembers()
    {
        var trainerId = GetCurrentTrainerId();
        var members = await _trainerDashboardService.GetMyMembersAsync(trainerId);
        var vm = members.Select(m => new TrainerMemberListItem
        {
            MemberId = m.MemberId,
            Name = m.Name,
            MemberCode = m.MemberCode,
            TrainingCompleted = m.TrainingCompleted,
            TrainingTotal = m.TrainingTotal,
            NutritionCompleted = m.NutritionCompleted,
            NutritionTotal = m.NutritionTotal
        }).ToList();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var trainerId = GetCurrentTrainerId();
        var sessions = await _trainerDashboardService.GetUpcomingSessionsAsync(trainerId);
        return View(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> Commissions()
    {
        var dashboard = await _commissionService.GetMyDashboardAsync();
        var vm = new TrainerCommissionDashboardViewModel
        {
            TotalOwed = dashboard.TotalOwed,
            TotalPaid = dashboard.TotalPaid,
            RecentCommissions = dashboard.RecentCommissions.Select(c => new TrainerCommissionRowViewModel
            {
                Id = c.Id,
                MembershipId = c.MembershipId,
                BranchId = c.BranchId,
                Source = c.Source,
                Status = c.Status,
                Percentage = c.Percentage,
                CalculatedAmount = c.CalculatedAmount,
                IsPaid = c.IsPaid,
                CreatedAt = c.CreatedAt,
                PaidAt = c.PaidAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Trainer")]
    public async Task<IActionResult> FinancialDashboard()
    {
        var trainerId = GetCurrentTrainerId();
        if (string.IsNullOrWhiteSpace(trainerId))
        {
            return Forbid();
        }

        var dto = await _trainerService.GetTrainerFinancialProfileAsync(trainerId);
        var vm = new TrainerFinancialDashboardViewModel
        {
            TrainerName = dto.TrainerName,
            BranchName = dto.BranchName,
            TotalGeneratedCommission = dto.TotalGeneratedCommission,
            TotalPaidCommission = dto.TotalPaidCommission,
            TotalPendingCommission = dto.TotalPendingCommission,
            MembershipRevenueFromTrainerMembers = dto.MembershipRevenueFromTrainerMembers,
            SessionRevenue = dto.SessionRevenue,
            Commissions = dto.Commissions.Select(c => new TrainerFinancialCommissionRowViewModel
            {
                MemberName = c.MemberName,
                MembershipPlanName = c.MembershipPlanName,
                Source = c.Source,
                Amount = c.Amount,
                Status = c.Status,
                Date = c.Date
            }).ToList(),
            MembershipRevenues = dto.MembershipRevenues.Select(m => new TrainerFinancialMembershipRevenueRowViewModel
            {
                MemberName = m.MemberName,
                PlanName = m.PlanName,
                RevenueAmount = m.RevenueAmount,
                StartDate = m.StartDate
            }).ToList(),
            SessionEarnings = dto.SessionEarnings.Select(s => new TrainerFinancialSessionEarningRowViewModel
            {
                SessionTitle = s.SessionTitle,
                MemberName = s.MemberName,
                Price = s.Price,
                TrainerShare = s.TrainerShare,
                Date = s.Date
            }).ToList(),
            RecentTransactions = dto.RecentTransactions.Select(t => new TrainerFinancialTransactionRowViewModel
            {
                Type = t.Type,
                Description = t.Description,
                Amount = t.Amount,
                Status = t.Status,
                Date = t.Date
            }).ToList(),
            CommissionLast30Days = dto.CommissionLast30Days.Select(p => new TrainerFinancialTrendPointViewModel
            {
                Date = p.Date,
                Amount = p.Amount
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SessionAttendance(int id)
    {
        var trainerId = GetCurrentTrainerId();
        var session = await _trainerDashboardService.GetSessionAttendanceAsync(trainerId, id);
        if (session == null) return NotFound();
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttendance(int memberSessionId, bool attended)
    {
        var trainerId = GetCurrentTrainerId();
        var ok = await _trainerDashboardService.SetAttendanceAsync(trainerId, memberSessionId, attended);
        if (!ok) return Forbid();
        TempData["Success"] = "Attendance updated.";
        return RedirectToAction(nameof(Sessions));
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


