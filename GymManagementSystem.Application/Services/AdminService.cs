using System;
using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _context;

    public AdminService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<LoginAuditDto>> GetLoginAuditsAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.LoginAudits
            .Include(l => l.User)
            .OrderByDescending(l => l.LoginTime);

        var total = await query.CountAsync();
        var audits = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LoginAuditDto
            {
                Id = l.Id,
                Email = l.Email,
                UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "Unknown",
                IpAddress = l.IpAddress ?? "Unknown",
                LoginTime = l.LoginTime,
                LogoutTime = l.LogoutTime,
                IsSuccessful = l.IsSuccessful,
                FailureReason = l.FailureReason
            })
            .ToListAsync();

        return new PaginatedResult<LoginAuditDto>(audits, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.LoginAudits
            .Include(l => l.User)
            .Where(l => l.IsSuccessful && l.LogoutTime == null)
            .OrderByDescending(l => l.LoginTime)
            .ToListAsync();

        return sessions
            .Select(l => new ActiveSessionDto
            {
                AuditId = l.Id,
                Email = l.Email,
                UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "Unknown",
                IpAddress = l.IpAddress ?? "Unknown",
                UserAgent = l.UserAgent,
                LoginTime = l.LoginTime,
                Duration = now - l.LoginTime
            })
            .ToList();
    }

    public async Task<AssignTrainerLookupDto> GetAssignTrainerLookupsAsync()
    {
        var trainers = await _context.Trainers
            .OrderBy(t => t.FirstName).ThenBy(t => t.LastName)
            .Select(t => new UserLookupDto
            {
                Id = t.Id,
                DisplayName = $"{t.FirstName} {t.LastName}"
            })
            .ToListAsync();

        var members = await _context.Members
            .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
            .Select(m => new UserLookupDto
            {
                Id = m.Id,
                DisplayName = $"{m.MemberCode} - {m.FirstName} {m.LastName}",
                Code = m.MemberCode
            })
            .ToListAsync();

        return new AssignTrainerLookupDto
        {
            Trainers = trainers,
            Members = members
        };
    }
}

