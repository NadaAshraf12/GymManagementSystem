using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services
{
    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MemberService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MemberDto>> GetAllMembersAsync()
        {
            var repo = _unitOfWork.Repository<Member>();
            var query = repo.Query().Where(m => m.IsActive);
            var members = await repo.ToListAsync(query);
            return members.Adapt<List<MemberDto>>();
        }

        public async Task<MemberDto?> GetMemberByIdAsync(string id)
        {
            var repo = _unitOfWork.Repository<Member>();
            var query = repo.Query().Where(m => m.Id == id && m.IsActive);
            var member = await repo.FirstOrDefaultAsync(query);

            if (member == null) return null;

            return member.Adapt<MemberDto>();
        }

        public async Task<bool> CreateMemberAsync(MemberDto memberDto)
        {
            try
            {
                var repo = _unitOfWork.Repository<Member>();
                var emailExists = await repo.AnyAsync(m => m.Email == memberDto.Email);
                if (emailExists)
                {
                    return false;
                }
                var member = memberDto.Adapt<Member>();
                member.Id = Guid.NewGuid().ToString();
                member.MemberCode = GenerateMemberCode();
                member.UserName = memberDto.Email;
                member.NormalizedUserName = memberDto.Email?.ToUpperInvariant();
                member.NormalizedEmail = memberDto.Email?.ToUpperInvariant();
                member.SecurityStamp = Guid.NewGuid().ToString("N");
                member.ConcurrencyStamp = Guid.NewGuid().ToString();
                member.EmailConfirmed = true;
                await repo.AddAsync(member);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateMemberCode()
        {
            return "MEM" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public async Task<bool> UpdateMemberAsync(MemberDto memberDto)
        {
            var repo = _unitOfWork.Repository<Member>();
            var member = await repo.GetByIdAsync(memberDto.Id);
            if (member == null) return false;

            member.FirstName = memberDto.FirstName;
            member.LastName = memberDto.LastName;
            member.Email = memberDto.Email;
            member.PhoneNumber = memberDto.PhoneNumber;
            member.DateOfBirth = memberDto.DateOfBirth;
            member.Gender = memberDto.Gender;
            member.Address = memberDto.Address;
            member.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMemberAsync(string id)
        {
            var repo = _unitOfWork.Repository<Member>();
            var member = await repo.GetByIdAsync(id);
            if (member == null) return false;

            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
