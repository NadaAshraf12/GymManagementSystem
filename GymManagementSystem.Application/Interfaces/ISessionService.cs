using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ISessionService
{
    Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionDto dto);
    Task<WorkoutSessionDto> UpdateAsync(UpdateWorkoutSessionDto dto);
    Task<bool> BookMemberAsync(BookMemberToSessionDto dto);
    Task<SessionBookingResultDto> BookPaidSessionAsync(PaidSessionBookingDto dto);
    Task<bool> CancelBookingAsync(string memberId, int workoutSessionId);
    Task<IReadOnlyList<WorkoutSessionDto>> GetByTrainerAsync(string trainerId, DateTime? from = null, DateTime? to = null);
    Task<IReadOnlyList<WorkoutSessionDto>> GetAvailableForMemberAsync(string memberId, DateTime? from = null, DateTime? to = null);
    Task<IReadOnlyList<SessionPricingPreviewDto>> GetSessionPricingPreviewsAsync(string memberId, DateTime? from = null, DateTime? to = null);
    Task<MembershipBenefitsSnapshotDto> GetMembershipBenefitsSnapshotAsync(string memberId);
}


