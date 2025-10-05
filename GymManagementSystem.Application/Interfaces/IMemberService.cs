using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IMemberService
    {
        Task<List<MemberDto>> GetAllMembersAsync();
        Task<MemberDto?> GetMemberByIdAsync(string id);
        Task<bool> CreateMemberAsync(MemberDto memberDto);
        Task<bool> UpdateMemberAsync(MemberDto memberDto);
        Task<bool> DeleteMemberAsync(string id);
    }
}
