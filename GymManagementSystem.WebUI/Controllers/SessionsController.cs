using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
public class SessionsController : BaseApiController
{
    private readonly ISessionService _sessionService;
    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
    [Authorize(Policy = "TrainerOwnsResource")]
    public async Task<ActionResult<ApiResponse<WorkoutSessionDto>>> Create(CreateWorkoutSessionDto dto)
    {
        var result = await _sessionService.CreateAsync(dto);
        return ApiCreated(result, "Session created successfully.");
    }

    [HttpPost("book")]
    [Authorize(Policy = "SessionBookingAccess")]
    public async Task<ActionResult<ApiResponse<object>>> Book(BookMemberToSessionDto dto)
    {
        var ok = await _sessionService.BookMemberAsync(dto);
        if (!ok)
        {
            return ApiBadRequest<object>("Cannot book session.");
        }

        return ApiOk<object>(null, "Session booked successfully.");
    }

    [HttpPost("book-paid")]
    [Authorize(Policy = "SessionBookingAccess")]
    public async Task<ActionResult<ApiResponse<SessionBookingResultDto>>> BookPaid(PaidSessionBookingDto dto)
    {
        var result = await _sessionService.BookPaidSessionAsync(dto);
        if (!result.Success)
        {
            return ApiBadRequest<SessionBookingResultDto>(result.Message);
        }

        return ApiOk(result, result.Message);
    }

    [HttpDelete("book")]
    [Authorize(Policy = "SessionBookingAccess")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel([FromQuery] string memberId, [FromQuery] int workoutSessionId)
    {
        var ok = await _sessionService.CancelBookingAsync(memberId, workoutSessionId);
        if (!ok)
        {
            return ApiNotFound<object>("Booking not found.");
        }

        return ApiOk<object>(null, "Booking cancelled successfully.");
    }

    [HttpGet("trainer/{trainerId}")]
    [Authorize(Policy = "TrainerOwnsResource")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkoutSessionDto>>>> GetByTrainer(string trainerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _sessionService.GetByTrainerAsync(trainerId, from, to);
        return ApiOk<IReadOnlyList<WorkoutSessionDto>>(list, "Sessions retrieved successfully.");
    }

    [HttpGet("member/{memberId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkoutSessionDto>>>> GetForMember(string memberId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _sessionService.GetAvailableForMemberAsync(memberId, from, to);
        return ApiOk<IReadOnlyList<WorkoutSessionDto>>(list, "Sessions retrieved successfully.");
    }

    [HttpGet("member/{memberId}/pricing-preview")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SessionPricingPreviewDto>>>> GetPricingPreview(string memberId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _sessionService.GetSessionPricingPreviewsAsync(memberId, from, to);
        return ApiOk<IReadOnlyList<SessionPricingPreviewDto>>(list, "Session pricing preview retrieved successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "TrainerOwnsResource")]
    public async Task<ActionResult<ApiResponse<WorkoutSessionDto>>> Update(int id, UpdateWorkoutSessionDto dto)
    {
        dto.Id = id;
        var updated = await _sessionService.UpdateAsync(dto);
        return ApiOk(updated, "Session updated successfully.");
    }
}


