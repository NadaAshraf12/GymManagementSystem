using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class TrainerAssignmentService : ITrainerAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrainerAssignmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AssignmentResultDto> AssignAsync(AssignTrainerDto dto)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();

        // Check if member already has a trainer
        var existingForMember = await assignmentRepo.FirstOrDefaultAsync(
            assignmentRepo.Query().Where(a => a.MemberId == dto.MemberId));

        if (existingForMember != null)
        {
            return new AssignmentResultDto
            {
                Success = false,
                Message = "This member is already assigned to another trainer."
            };
        }

        // Create new assignment
        var assignment = dto.Adapt<TrainerMemberAssignment>();
        assignment.AssignedAt = DateTime.UtcNow;

        await assignmentRepo.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return new AssignmentResultDto
        {
            Success = true,
            Message = "Trainer assigned successfully!"
        };
    }


    public async Task<bool> UnassignAsync(int assignmentId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var a = await assignmentRepo.FirstOrDefaultAsync(
            assignmentRepo.Query().Where(x => x.Id == assignmentId));
        if (a == null) return false;
        assignmentRepo.Remove(a);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountMembersForTrainerAsync(string trainerId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        return await assignmentRepo.Query().CountAsync(a => a.TrainerId == trainerId);
    }

    public async Task<IReadOnlyList<TrainerAssignmentDto>> GetAssignmentsForTrainerAsync(string trainerId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var list = await assignmentRepo.ToListAsync(
            assignmentRepo.Query()
                .Where(a => a.TrainerId == trainerId)
                .OrderByDescending(a => a.AssignedAt));

        return list.Adapt<List<TrainerAssignmentDto>>();
    }

    public async Task<IReadOnlyList<TrainerAssignmentDetailDto>> GetAssignmentsWithMembersAsync(string trainerId)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var assignments = await assignmentRepo.ToListAsync(
            assignmentRepo.Query()
                .Include(a => a.Member)
                .Where(a => a.TrainerId == trainerId)
                .OrderByDescending(a => a.AssignedAt));

        return assignments.Adapt<List<TrainerAssignmentDetailDto>>();
    }
}


