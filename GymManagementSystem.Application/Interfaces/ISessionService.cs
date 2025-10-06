using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ISessionService
{
    Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionDto dto);
    Task<bool> BookMemberAsync(BookMemberToSessionDto dto);
    Task<bool> CancelBookingAsync(string memberId, int workoutSessionId);
    Task<IReadOnlyList<WorkoutSessionDto>> GetByTrainerAsync(string trainerId, DateTime? from = null, DateTime? to = null);
}


