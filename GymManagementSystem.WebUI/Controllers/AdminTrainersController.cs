using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTrainersController : Controller
{
    private readonly ITrainerService _trainerService;
    private readonly ITrainerAssignmentService _assignmentService;

    public AdminTrainersController(ITrainerService trainerService, ITrainerAssignmentService assignmentService)
    {
        _trainerService = trainerService;
        _assignmentService = assignmentService;
    }

    public async Task<IActionResult> Index()
    {
        var trainers = await _trainerService.GetAllAsync();
        return View(trainers.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new TrainerDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _trainerService.CreateAsync(model);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var trainer = await _trainerService.GetByIdAsync(id);
        if (trainer == null) return NotFound();
        return View(trainer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TrainerDto model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var result = await _trainerService.UpdateAsync(model);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(string id)
    {
        var trainer = await _trainerService.GetByIdAsync(id);
        if (trainer == null) return NotFound();

        var assignments = await _assignmentService.GetAssignmentsWithMembersAsync(id);
        ViewBag.Trainer = trainer;
        return View(assignments.ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int assignmentId, string trainerId)
    {
        var removed = await _assignmentService.UnassignAsync(assignmentId);
        TempData[removed ? "Success" : "Error"] = removed
            ? "Member removed from trainer successfully."
            : "Assignment not found.";

        return RedirectToAction(nameof(Assignments), new { id = trainerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _trainerService.DeleteAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}