using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IMemberPlansService
{
    Task<MemberPlansSnapshotDto> GetSnapshotAsync(string memberId);
    Task<MemberFinancialProfileDto> GetMemberFinancialProfileAsync(string memberId);
    Task<bool> ToggleTrainingItemAsync(string memberId, int itemId);
    Task<bool> ToggleNutritionItemAsync(string memberId, int itemId);
}

public interface ITrainerDashboardService
{
    Task<IReadOnlyList<TrainerMemberProgressDto>> GetMyMembersAsync(string trainerId);
    Task<IReadOnlyList<WorkoutSessionDto>> GetUpcomingSessionsAsync(string trainerId);
    Task<SessionAttendanceDto?> GetSessionAttendanceAsync(string trainerId, int sessionId);
    Task<bool> SetAttendanceAsync(string trainerId, int memberSessionId, bool attended);
}
