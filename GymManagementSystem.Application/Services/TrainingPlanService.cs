using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;

namespace GymManagementSystem.Application.Services;

public class TrainingPlanService : ITrainingPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    public TrainingPlanService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(CreateTrainingPlanDto dto)
    {
        var plan = dto.Adapt<TrainingPlan>();
        plan.CreatedAt = DateTime.UtcNow;
        var planRepo = _unitOfWork.Repository<TrainingPlan>();
        await planRepo.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        if (dto.Items.Count > 0)
        {
            var items = dto.Items.Adapt<List<TrainingPlanItem>>();
            foreach (var item in items)
            {
                item.TrainingPlanId = plan.Id;
            }
            var itemRepo = _unitOfWork.Repository<TrainingPlanItem>();
            await itemRepo.AddRangeAsync(items);
            await _unitOfWork.SaveChangesAsync();
        }

        return plan.Id;
    }
}


