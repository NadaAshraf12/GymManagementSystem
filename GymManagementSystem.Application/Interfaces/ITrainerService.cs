using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ITrainerService
{
    Task<IReadOnlyList<TrainerDto>> GetAllAsync();
    Task<TrainerDto?> GetByIdAsync(string id);
    Task<OperationResultDto> CreateAsync(TrainerDto dto);
    Task<OperationResultDto> UpdateAsync(TrainerDto dto);
    Task<OperationResultDto> DeleteAsync(string id);
}

