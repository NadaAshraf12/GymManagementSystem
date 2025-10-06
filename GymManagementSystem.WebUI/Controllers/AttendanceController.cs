using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<AttendanceDto>> CheckIn(CheckInDto dto)
    {
        var result = await _attendanceService.CheckInAsync(dto);
        return Ok(result);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<AttendanceDto?>> CheckOut(CheckOutDto dto)
    {
        var result = await _attendanceService.CheckOutAsync(dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("member/{memberId}")]
    public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> GetMember(string memberId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _attendanceService.GetMemberAttendanceAsync(memberId, from, to);
        return Ok(list);
    }
}


