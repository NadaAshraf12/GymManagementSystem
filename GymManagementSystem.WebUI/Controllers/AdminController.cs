using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.WebUI.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly ITrainerAssignmentService _assignmentService;
    private readonly IAdminService _adminService;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ITrainerAssignmentService assignmentService,
        IAdminService adminService) : base(userManager)
    {
        _assignmentService = assignmentService;
        _adminService = adminService;
    }

    public async Task<IActionResult> LoginAudits(int page = 1, int pageSize = 20)
    {
        var result = await _adminService.GetLoginAuditsAsync(page, pageSize);
        return View(result);
    }

    public async Task<IActionResult> ActiveSessions()
    {
        var sessions = await _adminService.GetActiveSessionsAsync();
        return View(sessions);
    }

    public async Task<IActionResult> AssignTrainer()
    {
        var vm = await BuildAssignTrainerViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(AssignTrainerDto input)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please select both trainer and member.";
            var vmInvalid = await BuildAssignTrainerViewModelAsync();
            vmInvalid.Input = input;
            return View(vmInvalid);
        }

        var result = await _assignmentService.AssignAsync(input);

        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(AssignTrainer));
    }

    private async Task<AssignTrainerViewModel> BuildAssignTrainerViewModelAsync()
    {
        var lookups = await _adminService.GetAssignTrainerLookupsAsync();

        return new AssignTrainerViewModel
        {
            Trainers = lookups.Trainers.Select(t => new SelectListItem
            {
                Value = t.Id,
                Text = t.DisplayName
            }).ToList(),
            Members = lookups.Members.Select(m => new SelectListItem
            {
                Value = m.Id,
                Text = m.DisplayName
            }).ToList()
        };
    }
}
