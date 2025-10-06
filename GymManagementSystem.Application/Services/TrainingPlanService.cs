using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;

namespace GymManagementSystem.Application.Services;

public class TrainingPlanService : ITrainingPlanService
{
    private readonly IApplicationDbContext _db;
    public TrainingPlanService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateTrainingPlanDto dto)
    {
        var plan = new TrainingPlan
        {
            MemberId = dto.MemberId,
            TrainerId = dto.TrainerId,
            Title = dto.Title,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        _db.TrainingPlans.Add(plan);
        await _db.SaveChangesAsync();

        if (dto.Items.Count > 0)
        {
            foreach (var item in dto.Items)
            {
                _db.TrainingPlanItems.Add(new TrainingPlanItem
                {
                    TrainingPlanId = plan.Id,
                    DayOfWeek = item.DayOfWeek,
                    ExerciseName = item.ExerciseName,
                    Sets = item.Sets,
                    Reps = item.Reps,
                    Notes = item.Notes
                });
            }
            await _db.SaveChangesAsync();
        }

        return plan.Id;
    }
}


