using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TrainerAssignmentsController : ControllerBase
{
    private readonly ITrainerAssignmentService _service;
    public TrainerAssignmentsController(ITrainerAssignmentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Trainer")] 
    public async Task<ActionResult<TrainerAssignmentDto>> Assign(AssignTrainerDto dto)
    {
        var result = await _service.AssignAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Trainer")] 
    public async Task<IActionResult> Unassign(int id)
    {
        var ok = await _service.UnassignAsync(id);
        if (!ok) return NotFound();
        return Ok();
    }

    [HttpGet("trainer/{trainerId}")]
    public async Task<ActionResult<IReadOnlyList<TrainerAssignmentDto>>> GetForTrainer(string trainerId)
    {
        var list = await _service.GetAssignmentsForTrainerAsync(trainerId);
        return Ok(list);
    }

    [HttpGet("trainer/{trainerId}/count")]
    public async Task<ActionResult<int>> CountForTrainer(string trainerId)
    {
        var count = await _service.CountMembersForTrainerAsync(trainerId);
        return Ok(count);
    }
}


