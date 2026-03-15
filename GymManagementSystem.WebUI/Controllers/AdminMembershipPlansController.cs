using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/MembershipPlans/[action]")]
public class AdminMembershipPlansController : BaseController
{
    private readonly IMembershipPlanService _membershipPlanService;

    public AdminMembershipPlansController(
        IMembershipPlanService membershipPlanService,
        Microsoft.AspNetCore.Identity.UserManager<GymManagementSystem.Domain.Entities.ApplicationUser> userManager)
        : base(userManager)
    {
        _membershipPlanService = membershipPlanService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var plans = await _membershipPlanService.GetAllAsync();
        var vm = new MembershipPlansAdminIndexViewModel
        {
            Plans = plans.Select(p => new MembershipPlanReadDtoItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                DurationDays = p.DurationDays,
                Price = p.Price,
                DiscountPercentage = p.DiscountPercentage,
                CommissionRate = p.CommissionRate,
                IsActive = p.IsActive
            }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MembershipPlanFormViewModel
        {
            IsActive = true,
            DurationDays = 30
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MembershipPlanFormViewModel form)
    {
        try
        {
            await _membershipPlanService.CreateAsync(new CreateMembershipPlanDto
            {
                Name = form.Name,
                Description = form.Description,
                DurationDays = form.DurationDays,
                Price = form.Price,
                DiscountPercentage = form.DiscountPercentage,
                CommissionRate = form.CommissionRate,
                IsActive = form.IsActive,
                IncludedSessionsPerMonth = 0,
                PriorityBooking = false,
                AddOnAccess = true
            });

            TempData["Success"] = "Membership plan created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(form);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await _membershipPlanService.GetByIdAsync(id);
        var vm = new MembershipPlanFormViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            DurationDays = plan.DurationDays,
            Price = plan.Price,
            DiscountPercentage = plan.DiscountPercentage,
            CommissionRate = plan.CommissionRate,
            IsActive = plan.IsActive
        };

        return View(vm);
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MembershipPlanFormViewModel form)
    {
        if (id != form.Id)
        {
            TempData["Error"] = "Invalid plan id.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _membershipPlanService.UpdateAsync(new UpdateMembershipPlanDto
            {
                Id = form.Id,
                Name = form.Name,
                Description = form.Description,
                DurationDays = form.DurationDays,
                Price = form.Price,
                DiscountPercentage = form.DiscountPercentage,
                CommissionRate = form.CommissionRate,
                IsActive = form.IsActive,
                IncludedSessionsPerMonth = 0,
                PriorityBooking = false,
                AddOnAccess = true
            });

            TempData["Success"] = "Membership plan updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(form);
        }
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        try
        {
            await _membershipPlanService.ToggleActiveAsync(id);
            TempData["Success"] = "Plan status updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _membershipPlanService.SoftDeleteAsync(id);
            TempData["Success"] = "Plan deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
