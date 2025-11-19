using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ITrainerAssignmentService
{
    Task<AssignmentResultDto> AssignAsync(AssignTrainerDto dto);
    Task<bool> UnassignAsync(int assignmentId);
    Task<int> CountMembersForTrainerAsync(string trainerId);
    Task<IReadOnlyList<TrainerAssignmentDto>> GetAssignmentsForTrainerAsync(string trainerId);
    Task<IReadOnlyList<TrainerAssignmentDetailDto>> GetAssignmentsWithMembersAsync(string trainerId);
}


