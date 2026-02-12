using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class BranchesController : BaseApiController
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<BranchReadDto>>> Create(CreateBranchDto dto)
    {
        var created = await _branchService.CreateAsync(dto);
        return ApiCreated(created, "Branch created successfully.");
    }

    [HttpGet]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchReadDto>>>> GetAll()
    {
        var branches = await _branchService.GetAllAsync();
        return ApiOk<IReadOnlyList<BranchReadDto>>(branches, "Branches retrieved successfully.");
    }

    [HttpPost("assign-member")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<object>>> AssignMember(AssignUserBranchDto dto)
    {
        await _branchService.AssignMemberAsync(dto);
        return ApiOk<object>(new { }, "Member assigned to branch successfully.");
    }

    [HttpPost("assign-trainer")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<object>>> AssignTrainer(AssignUserBranchDto dto)
    {
        await _branchService.AssignTrainerAsync(dto);
        return ApiOk<object>(new { }, "Trainer assigned to branch successfully.");
    }
}
