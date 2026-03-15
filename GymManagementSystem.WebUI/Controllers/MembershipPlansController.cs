using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class MembershipPlansController : BaseApiController
{
    private readonly IMembershipPlanService _membershipPlanService;

    public MembershipPlansController(IMembershipPlanService membershipPlanService)
    {
        _membershipPlanService = membershipPlanService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipPlanReadDto>>> Create(CreateMembershipPlanDto dto)
    {
        var created = await _membershipPlanService.CreateAsync(dto);
        return ApiCreated(created, "Membership plan created successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipPlanReadDto>>> Update(int id, UpdateMembershipPlanDto dto)
    {
        dto.Id = id;
        var updated = await _membershipPlanService.UpdateAsync(dto);
        return ApiOk(updated, "Membership plan updated successfully.");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MembershipPlanReadDto>>>> GetAll([FromQuery] bool activeOnly = true)
    {
        var plans = activeOnly
            ? await _membershipPlanService.GetActiveAsync()
            : await _membershipPlanService.GetAllAsync();
        return ApiOk<IReadOnlyList<MembershipPlanReadDto>>(plans, "Membership plans retrieved successfully.");
    }

    [HttpPost("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<MembershipPlanReadDto>>> ToggleActive(int id)
    {
        var updated = await _membershipPlanService.ToggleActiveAsync(id);
        return ApiOk(updated, "Membership plan status updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(int id)
    {
        await _membershipPlanService.SoftDeleteAsync(id);
        return ApiOk<object>(new { }, "Membership plan deleted successfully.");
    }
}
