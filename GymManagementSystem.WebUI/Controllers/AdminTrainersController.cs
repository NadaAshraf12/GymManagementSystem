using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTrainersController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public AdminTrainersController(IApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
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
        if (!await _roleManager.RoleExistsAsync("Trainer"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Trainer"));
        }

        var trainer = new Trainer
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            Specialty = model.Specialty,
            Certification = model.Certification,
            Experience = model.Experience,
            Salary = model.Salary,
            BankAccount = model.BankAccount,
            HireDate = DateTime.UtcNow,
            MustChangePassword = true
        };

        var defaultPassword = "Gym@12345";
        var createResult = await _userManager.CreateAsync(trainer, defaultPassword);
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(trainer, "Trainer");
            TempData["Success"] = "Trainer created";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in createResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
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
        t.Email = model.Email;
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
    public async Task<IActionResult> RemoveMember(int assignmentId, string trainerId)
    {
        var assignment = await _db.TrainerMemberAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
        
        if (assignment == null)
        {
            TempData["Error"] = "Assignment not found.";
            return RedirectToAction(nameof(Assignments), new { id = trainerId });
        }

        _db.TrainerMemberAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        
        TempData["Success"] = "Member removed from trainer successfully.";
        return RedirectToAction(nameof(Assignments), new { id = trainerId });
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


