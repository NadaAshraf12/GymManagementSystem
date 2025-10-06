using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface INutritionPlanService
{
    Task<int> CreateAsync(CreateNutritionPlanDto dto);
}


