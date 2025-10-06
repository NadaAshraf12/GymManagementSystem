using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTrainersController : Controller
{
    private readonly IApplicationDbContext _db;
    public AdminTrainersController(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Trainers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Trainer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Trainer model)
    {
        if (!ModelState.IsValid) return View(model);
        // Minimal create: only profile fields; account creation typically via Identity pages
        _db.Trainers.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Trainer created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var t = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        return View(t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Trainer model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var t = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        t.FirstName = model.FirstName;
        t.LastName = model.LastName;
        t.Specialty = model.Specialty;
        t.Certification = model.Certification;
        t.Experience = model.Experience;
        t.Salary = model.Salary;
        t.BankAccount = model.BankAccount;
        t.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Trainer updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(string id)
    {
        var trainer = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
        if (trainer == null) return NotFound();
        var assignments = await _db.TrainerMemberAssignments
            .Include(a => a.Member)
            .Where(a => a.TrainerId == id)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();
        ViewBag.Trainer = trainer;
        return View(assignments);
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(string id)
{
    var trainer = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
    if (trainer == null)
        return NotFound();

    _db.Trainers.Remove(trainer);
    await _db.SaveChangesAsync();

    TempData["Success"] = "Trainer deleted successfully.";
    return RedirectToAction(nameof(Index));
}

}


