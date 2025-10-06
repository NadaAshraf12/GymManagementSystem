using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class SessionService : ISessionService
{
    private readonly IApplicationDbContext _db;
    public SessionService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionDto dto)
    {
        var session = new WorkoutSession
        {
            TrainerId = dto.TrainerId,
            Title = dto.Title,
            Description = dto.Description,
            SessionDate = dto.SessionDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            MaxParticipants = dto.MaxParticipants,
            CurrentParticipants = 0
        };

        _db.WorkoutSessions.Add(session);
        await _db.SaveChangesAsync();

        return Map(session);
    }

    public async Task<bool> BookMemberAsync(BookMemberToSessionDto dto)
    {
        var session = await _db.WorkoutSessions.FirstOrDefaultAsync(s => s.Id == dto.WorkoutSessionId);
        if (session == null)
            return false;

        if (session.CurrentParticipants >= session.MaxParticipants)
            return false;

        var alreadyBooked = await _db.MemberSessions.AnyAsync(ms => ms.WorkoutSessionId == dto.WorkoutSessionId && ms.MemberId == dto.MemberId);
        if (alreadyBooked)
            return true;

        var booking = new MemberSession
        {
            MemberId = dto.MemberId,
            WorkoutSessionId = dto.WorkoutSessionId,
            BookingDate = DateTime.UtcNow,
            Attended = false
        };
        _db.MemberSessions.Add(booking);
        session.CurrentParticipants += 1;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBookingAsync(string memberId, int workoutSessionId)
    {
        var booking = await _db.MemberSessions.FirstOrDefaultAsync(ms => ms.WorkoutSessionId == workoutSessionId && ms.MemberId == memberId);
        if (booking == null)
            return false;

        _db.MemberSessions.Remove(booking);
        var session = await _db.WorkoutSessions.FirstOrDefaultAsync(s => s.Id == workoutSessionId);
        if (session != null && session.CurrentParticipants > 0)
        {
            session.CurrentParticipants -= 1;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByTrainerAsync(string trainerId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.WorkoutSessions.AsQueryable().Where(s => s.TrainerId == trainerId);
        if (from.HasValue) query = query.Where(s => s.SessionDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SessionDate <= to.Value);
        var list = await query.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime).ToListAsync();
        return list.Select(Map).ToList();
    }

    private static WorkoutSessionDto Map(WorkoutSession s) => new()
    {
        Id = s.Id,
        TrainerId = s.TrainerId,
        Title = s.Title,
        Description = s.Description,
        SessionDate = s.SessionDate,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        MaxParticipants = s.MaxParticipants,
        CurrentParticipants = s.CurrentParticipants
    };
}


