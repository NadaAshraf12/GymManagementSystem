using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services
{
    public class MemberService : IMemberService
    {
        private readonly IApplicationDbContext _context;

        public MemberService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MemberDto>> GetAllMembersAsync()
        {
            var members = await _context.Members
                .Where(m => m.IsActive)
                .ToListAsync();

            return members.Select(m => new MemberDto
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email!,
                PhoneNumber = m.PhoneNumber!,
                DateOfBirth = m.DateOfBirth,
                Gender = m.Gender,
                Address = m.Address,
                MemberCode = m.MemberCode,
                IsActive = m.IsActive
            }).ToList();
        }

        public async Task<MemberDto?> GetMemberByIdAsync(string id)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (member == null) return null;

            return new MemberDto
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email!,
                PhoneNumber = member.PhoneNumber!,
                DateOfBirth = member.DateOfBirth,
                Gender = member.Gender,
                Address = member.Address,
                MemberCode = member.MemberCode,
                IsActive = member.IsActive
            };
        }

        public async Task<bool> CreateMemberAsync(MemberDto memberDto)
        {
            try
            {
                var member = new Member
                {
                    FirstName = memberDto.FirstName,
                    LastName = memberDto.LastName,
                    Email = memberDto.Email,
                    PhoneNumber = memberDto.PhoneNumber,
                    DateOfBirth = memberDto.DateOfBirth,
                    Gender = memberDto.Gender,
                    Address = memberDto.Address,
                    MemberCode = GenerateMemberCode(),
                    UserName = memberDto.Email,
                    EmailConfirmed = true
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

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
            var member = await _context.Members.FindAsync(memberDto.Id);
            if (member == null) return false;

            member.FirstName = memberDto.FirstName;
            member.LastName = memberDto.LastName;
            member.PhoneNumber = memberDto.PhoneNumber;
            member.DateOfBirth = memberDto.DateOfBirth;
            member.Gender = memberDto.Gender;
            member.Address = memberDto.Address;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMemberAsync(string id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null) return false;

            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
