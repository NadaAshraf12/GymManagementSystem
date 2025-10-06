using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
    [Authorize(Roles = "Trainer,Admin")]
    public async Task<ActionResult<WorkoutSessionDto>> Create(CreateWorkoutSessionDto dto)
    {
        var result = await _sessionService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPost("book")]
    [Authorize(Roles = "Member,Admin,Trainer")]
    public async Task<IActionResult> Book(BookMemberToSessionDto dto)
    {
        var ok = await _sessionService.BookMemberAsync(dto);
        if (!ok) return BadRequest("Cannot book session");
        return Ok();
    }

    [HttpDelete("book")]
    [Authorize(Roles = "Member,Admin,Trainer")]
    public async Task<IActionResult> Cancel([FromQuery] string memberId, [FromQuery] int workoutSessionId)
    {
        var ok = await _sessionService.CancelBookingAsync(memberId, workoutSessionId);
        if (!ok) return NotFound();
        return Ok();
    }

    [HttpGet("trainer/{trainerId}")]
    public async Task<ActionResult<IReadOnlyList<WorkoutSessionDto>>> GetByTrainer(string trainerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _sessionService.GetByTrainerAsync(trainerId, from, to);
        return Ok(list);
    }
}


