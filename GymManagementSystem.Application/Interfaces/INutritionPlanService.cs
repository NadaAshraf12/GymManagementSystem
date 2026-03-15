using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface INutritionPlanService
{
    Task<int> CreateAsync(CreateNutritionPlanDto dto);
    Task<NutritionPlanDto> UpdateAsync(UpdateNutritionPlanDto dto);
    Task<NutritionPlanItemDto> UpdateItemAsync(UpdateNutritionPlanItemDto dto);
    Task<NutritionPlanDto> GetByIdAsync(int id);
}


