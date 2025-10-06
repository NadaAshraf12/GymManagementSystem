using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ITrainingPlanService
{
    Task<int> CreateAsync(CreateTrainingPlanDto dto);
}


