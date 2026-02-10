using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;

    public SessionService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionDto dto)
    {
        var session = dto.Adapt<WorkoutSession>();
        session.CurrentParticipants = 0;

        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        await sessionRepo.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return session.Adapt<WorkoutSessionDto>();
    }

    public async Task<WorkoutSessionDto> UpdateAsync(UpdateWorkoutSessionDto dto)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == dto.Id));

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(session.TrainerId);

        dto.Adapt(session);
        await _unitOfWork.SaveChangesAsync();

        return session.Adapt<WorkoutSessionDto>();
    }

    public async Task<bool> BookMemberAsync(BookMemberToSessionDto dto)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();

        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == dto.WorkoutSessionId));
        if (session == null)
            return false;

        if (session.CurrentParticipants >= session.MaxParticipants)
            return false;

        var alreadyBooked = await memberSessionRepo.AnyAsync(ms =>
            ms.WorkoutSessionId == dto.WorkoutSessionId && ms.MemberId == dto.MemberId);
        if (alreadyBooked)
            return true;

        var booking = new MemberSession
        {
            MemberId = dto.MemberId,
            WorkoutSessionId = dto.WorkoutSessionId,
            BookingDate = DateTime.UtcNow,
            Attended = false
        };
        await memberSessionRepo.AddAsync(booking);
        session.CurrentParticipants += 1;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBookingAsync(string memberId, int workoutSessionId)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();

        var booking = await memberSessionRepo.FirstOrDefaultAsync(
            memberSessionRepo.Query().Where(ms => ms.WorkoutSessionId == workoutSessionId && ms.MemberId == memberId));
        if (booking == null)
            return false;

        memberSessionRepo.Remove(booking);
        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == workoutSessionId));
        if (session != null && session.CurrentParticipants > 0)
        {
            session.CurrentParticipants -= 1;
        }
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByTrainerAsync(string trainerId, DateTime? from = null, DateTime? to = null)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var query = sessionRepo.Query().Where(s => s.TrainerId == trainerId);
        if (from.HasValue) query = query.Where(s => s.SessionDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SessionDate <= to.Value);
        var list = await sessionRepo.ToListAsync(query.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime));
        return list.Adapt<List<WorkoutSessionDto>>();
    }
}


