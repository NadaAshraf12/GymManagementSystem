using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly IMemberService _memberService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public MembersController(IMemberService memberService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _memberService = memberService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var members = await _memberService.GetAllMembersAsync();
        return View(members);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberDto memberDto)
    {
        if (!ModelState.IsValid)
        {
            return View(memberDto);
        }

        if (!await _roleManager.RoleExistsAsync("Member"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Member"));
        }

        var member = new Member
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = memberDto.FirstName,
            LastName = memberDto.LastName,
            Email = memberDto.Email,
            UserName = memberDto.Email,
            PhoneNumber = memberDto.PhoneNumber,
            DateOfBirth = memberDto.DateOfBirth,
            Gender = memberDto.Gender,
            Address = memberDto.Address,
            MemberCode = "MEM" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            EmailConfirmed = true
        };

        // Use unified default password and require change on first login
        member.MustChangePassword = true;
        var tempPassword = "Gym@12345"; 
        var result = await _userManager.CreateAsync(member, tempPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(member, "Member");
            TempData["Success"] = "Member created successfully";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(memberDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var member = await _memberService.GetMemberByIdAsync(id);
        if (member == null)
        {
            return NotFound();
        }
        return View(member);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MemberDto memberDto)
    {
        if (!ModelState.IsValid)
        {
            return View(memberDto);
        }

        var ok = await _memberService.UpdateMemberAsync(memberDto);
        if (ok)
        {
            TempData["Success"] = "Member updated successfully";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "Failed to update member");
        return View(memberDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _memberService.DeleteMemberAsync(id);
        if (ok)
        {
            TempData["Success"] = "Member deleted successfully";
        }
        else
        {
            TempData["Error"] = "Failed to delete member";
        }
        return RedirectToAction("Index");
    }
}