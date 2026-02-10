using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface ITrainerService
{
    Task<IReadOnlyList<TrainerReadDto>> GetAllAsync();
    Task<UpdateTrainerDto?> GetByIdAsync(string id);
    Task<OperationResultDto> CreateAsync(CreateTrainerDto dto);
    Task<OperationResultDto> UpdateAsync(UpdateTrainerDto dto);
    Task<OperationResultDto> DeleteAsync(string id);
}

