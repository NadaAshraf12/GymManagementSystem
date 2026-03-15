using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IMembershipPlanService
{
    Task<IReadOnlyList<MembershipPlanReadDto>> GetAllAsync();
    Task<IReadOnlyList<MembershipPlanReadDto>> GetActiveAsync();
    Task<MembershipPlanReadDto> GetByIdAsync(int id);
    Task<MembershipPlanReadDto> CreateAsync(CreateMembershipPlanDto dto);
    Task<MembershipPlanReadDto> UpdateAsync(UpdateMembershipPlanDto dto);
    Task<MembershipPlanReadDto> ToggleActiveAsync(int id);
    Task SoftDeleteAsync(int id);
}
