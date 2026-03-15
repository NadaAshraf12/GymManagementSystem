using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IMemberService
    {
        Task<List<MemberReadDto>> GetAllMembersAsync();
        Task<UpdateMemberDto?> GetMemberByIdAsync(string id);
        Task<bool> CreateMemberAsync(CreateMemberDto memberDto);
        Task<bool> UpdateMemberAsync(UpdateMemberDto memberDto);
        Task<bool> DeleteMemberAsync(string id);
    }
}
