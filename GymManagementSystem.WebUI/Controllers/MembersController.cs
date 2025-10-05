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

    // GET: /Members
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var members = await _memberService.GetAllMembersAsync();
        return View(members);
    }

    // GET: /Members/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Members/Create
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
}