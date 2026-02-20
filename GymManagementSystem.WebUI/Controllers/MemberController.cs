using System.Security.Claims;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Member,Admin")] 
public class MemberController : BaseController
{
    private readonly ISessionService _sessionService;
    private readonly IMemberPlansService _memberPlansService;
    private readonly IMembershipService _membershipService;

    public MemberController(
        ISessionService sessionService,
        IMemberPlansService memberPlansService,
        IMembershipService membershipService,
        UserManager<ApplicationUser> userManager) : base(userManager)
    {
        _sessionService = sessionService;
        _memberPlansService = memberPlansService;
        _membershipService = membershipService;
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
        var memberships = await _membershipService.GetMembershipsForMemberAsync(memberId);
        ViewBag.CurrentMembership = memberships
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefault(m => m.Status is Domain.Enums.MembershipStatus.Active
                or Domain.Enums.MembershipStatus.PendingPayment
                or Domain.Enums.MembershipStatus.Expired);

        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> FinancialProfile()
    {
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var dto = await _memberPlansService.GetMemberFinancialProfileAsync(memberId);

        var vm = new MemberFinancialProfileViewModel
        {
            WalletBalance = dto.WalletBalance,
            WalletTransactions = dto.WalletTransactions.Select(x => new MemberFinancialWalletRowViewModel
            {
                Date = x.Date,
                Type = x.Type,
                Amount = x.Amount,
                Description = x.Description,
                RunningBalance = x.RunningBalance
            }).ToList(),
            Purchases = dto.Purchases.Select(x => new MemberFinancialPurchaseRowViewModel
            {
                Date = x.Date,
                Category = x.Category,
                Amount = x.Amount,
                Description = x.Description,
                InvoiceNumber = x.InvoiceNumber
            }).ToList(),
            MembershipHistory = dto.MembershipHistory.Select(x => new MemberFinancialMembershipRowViewModel
            {
                MembershipId = x.MembershipId,
                PlanName = x.PlanName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                Source = x.Source
            }).ToList()
        };

        return View(vm);
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


