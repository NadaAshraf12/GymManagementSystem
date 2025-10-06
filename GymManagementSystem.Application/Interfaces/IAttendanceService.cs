using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceDto> CheckInAsync(CheckInDto dto);
    Task<AttendanceDto?> CheckOutAsync(CheckOutDto dto);
    Task<IReadOnlyList<AttendanceDto>> GetMemberAttendanceAsync(string memberId, DateTime? from = null, DateTime? to = null);
}


