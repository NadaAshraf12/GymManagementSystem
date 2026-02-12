using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class MemberPlansService : IMemberPlansService
{
    private readonly IUnitOfWork _unitOfWork;

    public MemberPlansService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberPlansSnapshotDto> GetSnapshotAsync(string memberId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var trainingRepo = _unitOfWork.Repository<TrainingPlan>();
        var nutritionRepo = _unitOfWork.Repository<NutritionPlan>();
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();

        var assignment = await assignmentRepo.FirstOrDefaultAsync(
            assignmentRepo.Query()
                .AsNoTracking()
                .Include(a => a.Trainer)
                .Where(a => a.MemberId == memberId));

        var latestTraining = await trainingRepo.FirstOrDefaultAsync(
            trainingRepo.Query()
                .AsNoTracking()
                .Where(tp => tp.MemberId == memberId)
                .OrderByDescending(tp => tp.CreatedAt)
                .Include(tp => tp.Items));

        var latestNutrition = await nutritionRepo.FirstOrDefaultAsync(
            nutritionRepo.Query()
                .AsNoTracking()
                .Where(np => np.MemberId == memberId)
                .OrderByDescending(np => np.CreatedAt)
                .Include(np => np.Items));

        var upcoming = await memberSessionRepo.ToListAsync(
            memberSessionRepo.Query()
                .AsNoTracking()
                .Include(ms => ms.WorkoutSession)
                .Where(ms => ms.MemberId == memberId && ms.WorkoutSession.SessionDate >= DateTime.UtcNow.Date)
                .OrderBy(ms => ms.WorkoutSession.SessionDate)
                .Take(10));

        List<WorkoutSessionDto> trainerUpcoming = new();
        if (assignment != null)
        {
            var sessions = await sessionRepo.ToListAsync(
                sessionRepo.Query()
                    .AsNoTracking()
                    .Where(ws => ws.TrainerId == assignment.TrainerId && ws.SessionDate >= DateTime.UtcNow.Date)
                    .OrderBy(ws => ws.SessionDate)
                    .ThenBy(ws => ws.StartTime)
                    .Take(20));
            trainerUpcoming = sessions.Adapt<List<WorkoutSessionDto>>();
        }

        var bookedIds = upcoming.Select(ms => ms.WorkoutSessionId).ToHashSet();
        return new MemberPlansSnapshotDto
        {
            Assignment = assignment == null ? null : new MemberPlanAssignmentDto
            {
                TrainerId = assignment.TrainerId,
                TrainerName = $"{assignment.Trainer.FirstName} {assignment.Trainer.LastName}",
                Specialty = assignment.Trainer.Specialty
            },
            TrainingPlan = latestTraining?.Adapt<TrainingPlanDto>(),
            NutritionPlan = latestNutrition?.Adapt<NutritionPlanDto>(),
            UpcomingBookings = upcoming.Select(x => new MemberUpcomingBookingDto
            {
                WorkoutSessionId = x.WorkoutSessionId,
                BookingDate = x.BookingDate
            }).ToList(),
            TrainerUpcomingSessions = trainerUpcoming,
            BookedSessionIds = bookedIds
        };
    }

    public async Task<bool> ToggleTrainingItemAsync(string memberId, int itemId)
    {
        var itemRepo = _unitOfWork.Repository<TrainingPlanItem>();
        var item = await itemRepo.FirstOrDefaultAsync(
            itemRepo.Query()
                .Include(i => i.TrainingPlan)
                .Where(i => i.Id == itemId));
        if (item == null || item.TrainingPlan.MemberId != memberId)
        {
            return false;
        }

        item.IsCompleted = !item.IsCompleted;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleNutritionItemAsync(string memberId, int itemId)
    {
        var itemRepo = _unitOfWork.Repository<NutritionPlanItem>();
        var item = await itemRepo.FirstOrDefaultAsync(
            itemRepo.Query()
                .Include(i => i.NutritionPlan)
                .Where(i => i.Id == itemId));
        if (item == null || item.NutritionPlan.MemberId != memberId)
        {
            return false;
        }

        item.IsCompleted = !item.IsCompleted;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public class TrainerDashboardService : ITrainerDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrainerDashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TrainerMemberProgressDto>> GetMyMembersAsync(string trainerId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var assignments = await assignmentRepo.ToListAsync(
            assignmentRepo.Query()
                .AsNoTracking()
                .Where(a => a.TrainerId == trainerId));

        var memberIds = assignments.Select(a => a.MemberId).Distinct().ToList();
        if (memberIds.Count == 0)
        {
            return Array.Empty<TrainerMemberProgressDto>();
        }

        var membersRepo = _unitOfWork.Repository<Member>();
        var tpItemRepo = _unitOfWork.Repository<TrainingPlanItem>();
        var npItemRepo = _unitOfWork.Repository<NutritionPlanItem>();

        return await membersRepo.Query()
            .AsNoTracking()
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new TrainerMemberProgressDto
            {
                MemberId = m.Id,
                Name = m.FirstName + " " + m.LastName,
                MemberCode = m.MemberCode,
                TrainingCompleted = tpItemRepo.Query()
                    .Where(i => i.TrainingPlan.MemberId == m.Id && i.IsCompleted)
                    .Count(),
                TrainingTotal = tpItemRepo.Query()
                    .Count(i => i.TrainingPlan.MemberId == m.Id),
                NutritionCompleted = npItemRepo.Query()
                    .Where(i => i.NutritionPlan.MemberId == m.Id && i.IsCompleted)
                    .Count(),
                NutritionTotal = npItemRepo.Query()
                    .Count(i => i.NutritionPlan.MemberId == m.Id)
            })
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetUpcomingSessionsAsync(string trainerId)
    {
        var repo = _unitOfWork.Repository<WorkoutSession>();
        var sessions = await repo.ToListAsync(
            repo.Query()
                .AsNoTracking()
                .Where(ws => ws.TrainerId == trainerId && ws.SessionDate >= DateTime.UtcNow.Date)
                .OrderBy(ws => ws.SessionDate)
                .ThenBy(ws => ws.StartTime));
        return sessions.Adapt<List<WorkoutSessionDto>>();
    }

    public async Task<SessionAttendanceDto?> GetSessionAttendanceAsync(string trainerId, int sessionId)
    {
        var repo = _unitOfWork.Repository<WorkoutSession>();
        var session = await repo.FirstOrDefaultAsync(
            repo.Query()
                .AsNoTracking()
                .Include(ws => ws.MemberSessions)
                .ThenInclude(ms => ms.Member)
                .Where(ws => ws.Id == sessionId && ws.TrainerId == trainerId));

        if (session == null)
        {
            return null;
        }

        return new SessionAttendanceDto
        {
            SessionId = session.Id,
            Title = session.Title,
            SessionDate = session.SessionDate,
            Items = session.MemberSessions
                .OrderBy(ms => ms.BookingDate)
                .Select(ms => new SessionAttendanceItemDto
                {
                    MemberSessionId = ms.Id,
                    MemberId = ms.MemberId,
                    MemberName = $"{ms.Member.FirstName} {ms.Member.LastName}",
                    BookingDate = ms.BookingDate,
                    Attended = ms.Attended
                }).ToList()
        };
    }

    public async Task<bool> SetAttendanceAsync(string trainerId, int memberSessionId, bool attended)
    {
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();
        var memberSession = await memberSessionRepo.FirstOrDefaultAsync(
            memberSessionRepo.Query()
                .Include(ms => ms.WorkoutSession)
                .Where(ms => ms.Id == memberSessionId));
        if (memberSession == null || memberSession.WorkoutSession.TrainerId != trainerId)
        {
            return false;
        }

        memberSession.Attended = attended;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
