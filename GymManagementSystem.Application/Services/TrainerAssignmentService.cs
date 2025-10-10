using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class TrainerAssignmentService : ITrainerAssignmentService
{
    private readonly IApplicationDbContext _db;
    public TrainerAssignmentService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AssignmentResultDto> AssignAsync(AssignTrainerDto dto)
    {
        // Check if member already has a trainer
        var existingForMember = await _db.TrainerMemberAssignments
            .FirstOrDefaultAsync(a => a.MemberId == dto.MemberId);

        if (existingForMember != null)
        {
            return new AssignmentResultDto
            {
                Success = false,
                Message = "This member is already assigned to another trainer."
            };
        }

        // Create new assignment
        var assignment = new TrainerMemberAssignment
        {
            TrainerId = dto.TrainerId,
            MemberId = dto.MemberId,
            Notes = dto.Notes,
            AssignedAt = DateTime.UtcNow
        };

        _db.TrainerMemberAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return new AssignmentResultDto
        {
            Success = true,
            Message = "Trainer assigned successfully!"
        };
    }


    public async Task<bool> UnassignAsync(int assignmentId)
    {
        var a = await _db.TrainerMemberAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId);
        if (a == null) return false;
        _db.TrainerMemberAssignments.Remove(a);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountMembersForTrainerAsync(string trainerId)
    {
        return await _db.TrainerMemberAssignments.CountAsync(a => a.TrainerId == trainerId);
    }

    public async Task<IReadOnlyList<TrainerAssignmentDto>> GetAssignmentsForTrainerAsync(string trainerId)
    {
        var list = await _db.TrainerMemberAssignments.Where(a => a.TrainerId == trainerId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    private static TrainerAssignmentDto Map(TrainerMemberAssignment a) => new()
    {
        Id = a.Id,
        TrainerId = a.TrainerId,
        MemberId = a.MemberId,
        AssignedAt = a.AssignedAt,
        Notes = a.Notes
    };
}


