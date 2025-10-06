using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IApplicationDbContext _db;
    public AttendanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AttendanceDto> CheckInAsync(CheckInDto dto)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.MemberId == dto.MemberId && a.Date == today && a.CheckOutTime == null);
        if (existing != null)
        {
            return Map(existing);
        }

        var attendance = new Attendance
        {
            MemberId = dto.MemberId,
            CheckInTime = DateTime.UtcNow,
            Date = today
        };
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();
        return Map(attendance);
    }

    public async Task<AttendanceDto?> CheckOutAsync(CheckOutDto dto)
    {
        var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.Id == dto.AttendanceId);
        if (attendance == null)
            return null;

        if (attendance.CheckOutTime == null)
        {
            attendance.CheckOutTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Map(attendance);
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetMemberAttendanceAsync(string memberId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Attendances.AsQueryable().Where(a => a.MemberId == memberId);
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value.Date);
        var list = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.CheckInTime).ToListAsync();
        return list.Select(Map).ToList();
    }

    private static AttendanceDto Map(Attendance a) => new()
    {
        Id = a.Id,
        MemberId = a.MemberId,
        CheckInTime = a.CheckInTime,
        CheckOutTime = a.CheckOutTime,
        Date = a.Date
    };
}


