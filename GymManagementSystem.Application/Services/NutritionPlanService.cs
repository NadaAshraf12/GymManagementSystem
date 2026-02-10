using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class NutritionPlanService : INutritionPlanService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IAppAuthorizationService _authorizationService;

    public NutritionPlanService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<int> CreateAsync(CreateNutritionPlanDto dto)
    {
        var plan = dto.Adapt<NutritionPlan>();
        plan.CreatedAt = DateTime.UtcNow;

        var planRepo = _unitOfWork.Repository<NutritionPlan>();
        await planRepo.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        var itemsDto = dto.Items ?? new List<CreateNutritionPlanItemDto>();
        if (itemsDto.Count > 0)
        {
            var items = itemsDto.Adapt<List<NutritionPlanItem>>();
            foreach (var item in items)
            {
                item.NutritionPlanId = plan.Id;
            }

            var itemRepo = _unitOfWork.Repository<NutritionPlanItem>();
            await itemRepo.AddRangeAsync(items);
            await _unitOfWork.SaveChangesAsync();
        }

        return plan.Id;
    }

    public async Task<NutritionPlanDto> UpdateAsync(UpdateNutritionPlanDto dto)
    {
        var planRepo = _unitOfWork.Repository<NutritionPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(
            planRepo.Query().Where(p => p.Id == dto.Id));

        if (plan == null)
        {
            throw new KeyNotFoundException("Nutrition plan not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(plan.TrainerId);

        dto.Adapt(plan);
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<NutritionPlanDto>();
    }

    public async Task<NutritionPlanItemDto> UpdateItemAsync(UpdateNutritionPlanItemDto dto)
    {
        var itemRepo = _unitOfWork.Repository<NutritionPlanItem>();
        var item = await itemRepo.FirstOrDefaultAsync(
            itemRepo.Query()
                .Include(i => i.NutritionPlan)
                .Where(i => i.Id == dto.Id));

        if (item == null)
        {
            throw new KeyNotFoundException("Nutrition plan item not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(item.NutritionPlan.TrainerId);

        dto.Adapt(item);
        await _unitOfWork.SaveChangesAsync();

        return item.Adapt<NutritionPlanItemDto>();
    }

    public async Task<NutritionPlanDto> GetByIdAsync(int id)
    {
        var planRepo = _unitOfWork.Repository<NutritionPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(
            planRepo.Query()
                .Include(p => p.Items)
                .Where(p => p.Id == id));

        if (plan == null)
        {
            throw new KeyNotFoundException("Nutrition plan not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(plan.TrainerId);

        return plan.Adapt<NutritionPlanDto>();
    }
}


