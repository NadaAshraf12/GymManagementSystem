using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ITrainingPlanService
{
    Task<int> CreateAsync(CreateTrainingPlanDto dto);
    Task<TrainingPlanDto> UpdateAsync(UpdateTrainingPlanDto dto);
    Task<TrainingPlanItemDto> UpdateItemAsync(UpdateTrainingPlanItemDto dto);
    Task<TrainingPlanDto> GetByIdAsync(int id);
}


