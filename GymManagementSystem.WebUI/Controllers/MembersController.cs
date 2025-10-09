using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
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

        var ok = await _memberService.CreateMemberAsync(memberDto);
        if (ok)
        {
            TempData["Success"] = "Member created successfully";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "Failed to create member");
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