using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Policy = "TrainerOwnsResource")]
public class TrainerAssignmentsController : BaseApiController
{
    private readonly ITrainerAssignmentService _service;
    public TrainerAssignmentsController(ITrainerAssignmentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssignmentResultDto>>> Assign(AssignTrainerDto dto)
    {
        var result = await _service.AssignAsync(dto);
        if (!result.Success)
        {
            return ApiBadRequest<AssignmentResultDto>(result.Message);
        }

        return ApiCreated(result, "Trainer assigned successfully.");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Unassign(int id)
    {
        var ok = await _service.UnassignAsync(id);
        if (!ok)
        {
            return ApiNotFound<object>("Assignment not found.");
        }

        return ApiOk<object>(null, "Assignment removed successfully.");
    }

    [HttpGet("trainer/{trainerId}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainerAssignmentDto>>>> GetForTrainer(string trainerId)
    {
        var list = await _service.GetAssignmentsForTrainerAsync(trainerId);
        return ApiOk<IReadOnlyList<TrainerAssignmentDto>>(list, "Assignments retrieved successfully.");
    }

    [HttpGet("trainer/{trainerId}/count")]
    public async Task<ActionResult<ApiResponse<int>>> CountForTrainer(string trainerId)
    {
        var count = await _service.CountMembersForTrainerAsync(trainerId);
        return ApiOk(count, "Assignment count retrieved successfully.");
    }
}


