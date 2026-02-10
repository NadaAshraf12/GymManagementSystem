using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class TrainingPlanService : ITrainingPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    public TrainingPlanService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<int> CreateAsync(CreateTrainingPlanDto dto)
    {
        var plan = dto.Adapt<TrainingPlan>();
        plan.CreatedAt = DateTime.UtcNow;
        var planRepo = _unitOfWork.Repository<TrainingPlan>();
        await planRepo.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        var itemsDto = dto.Items ?? new List<CreateTrainingPlanItemDto>();
        if (itemsDto.Count > 0)
        {
            var items = itemsDto.Adapt<List<TrainingPlanItem>>();
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

    public async Task<TrainingPlanDto> UpdateAsync(UpdateTrainingPlanDto dto)
    {
        var planRepo = _unitOfWork.Repository<TrainingPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(
            planRepo.Query().Where(p => p.Id == dto.Id));

        if (plan == null)
        {
            throw new KeyNotFoundException("Training plan not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(plan.TrainerId);

        dto.Adapt(plan);
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<TrainingPlanDto>();
    }

    public async Task<TrainingPlanItemDto> UpdateItemAsync(UpdateTrainingPlanItemDto dto)
    {
        var itemRepo = _unitOfWork.Repository<TrainingPlanItem>();
        var item = await itemRepo.FirstOrDefaultAsync(
            itemRepo.Query()
                .Include(i => i.TrainingPlan)
                .Where(i => i.Id == dto.Id));

        if (item == null)
        {
            throw new KeyNotFoundException("Training plan item not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(item.TrainingPlan.TrainerId);

        dto.Adapt(item);
        await _unitOfWork.SaveChangesAsync();

        return item.Adapt<TrainingPlanItemDto>();
    }

    public async Task<TrainingPlanDto> GetByIdAsync(int id)
    {
        var planRepo = _unitOfWork.Repository<TrainingPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(
            planRepo.Query()
                .Include(p => p.Items)
                .Where(p => p.Id == id));

        if (plan == null)
        {
            throw new KeyNotFoundException("Training plan not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(plan.TrainerId);

        return plan.Adapt<TrainingPlanDto>();
    }
}


