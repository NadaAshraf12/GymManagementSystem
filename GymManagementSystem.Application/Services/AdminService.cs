using System;
using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedResult<LoginAuditDto>> GetLoginAuditsAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var loginAuditRepo = _unitOfWork.Repository<LoginAudit>();
        var query = loginAuditRepo.Query()
            .Include(l => l.User)
            .OrderByDescending(l => l.LoginTime);

        var total = await query.CountAsync();
        var audits = await loginAuditRepo.ToListAsync(
            query.Skip((page - 1) * pageSize).Take(pageSize));

        var dtos = audits.Adapt<List<LoginAuditDto>>();
        return new PaginatedResult<LoginAuditDto>(dtos, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var loginAuditRepo = _unitOfWork.Repository<LoginAudit>();
        var sessions = await loginAuditRepo.ToListAsync(
            loginAuditRepo.Query()
                .Include(l => l.User)
                .Where(l => l.IsSuccessful && l.LogoutTime == null)
                .OrderByDescending(l => l.LoginTime));

        var dtos = sessions.Adapt<List<ActiveSessionDto>>();
        foreach (var dto in dtos)
        {
            dto.Duration = now - dto.LoginTime;
        }
        return dtos;
    }

    public async Task<AssignTrainerLookupDto> GetAssignTrainerLookupsAsync()
    {
        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var memberRepo = _unitOfWork.Repository<Member>();

        var trainers = await trainerRepo.ToListAsync(
            trainerRepo.Query()
                .OrderBy(t => t.FirstName).ThenBy(t => t.LastName));

        var members = await memberRepo.ToListAsync(
            memberRepo.Query()
                .OrderBy(m => m.FirstName).ThenBy(m => m.LastName));

        return new AssignTrainerLookupDto
        {
            Trainers = trainers.Adapt<List<UserLookupDto>>(),
            Members = members.Adapt<List<UserLookupDto>>()
        };
    }
}

