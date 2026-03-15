using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class MembershipPlanService : IMembershipPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;

    public MembershipPlanService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<IReadOnlyList<MembershipPlanReadDto>> GetAllAsync()
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plans = await repo.ToListAsync(repo.Query().AsNoTracking().OrderBy(p => p.Name));
        return plans.Adapt<List<MembershipPlanReadDto>>();
    }

    public async Task<IReadOnlyList<MembershipPlanReadDto>> GetActiveAsync()
    {
        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plans = await repo.ToListAsync(
            repo.Query().AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Price));
        return plans.Adapt<List<MembershipPlanReadDto>>();
    }

    public async Task<MembershipPlanReadDto> GetByIdAsync(int id)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plan = await repo.FirstOrDefaultAsync(repo.Query().AsNoTracking().Where(p => p.Id == id));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found.");
        }

        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task<MembershipPlanReadDto> CreateAsync(CreateMembershipPlanDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        await EnsureUniqueNameAsync(dto.Name);

        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plan = dto.Adapt<MembershipPlan>();
        await repo.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task<MembershipPlanReadDto> UpdateAsync(UpdateMembershipPlanDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();
        await EnsureUniqueNameAsync(dto.Name, dto.Id);

        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plan = await repo.FirstOrDefaultAsync(repo.Query().Where(p => p.Id == dto.Id));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found.");
        }

        dto.Adapt(plan);
        plan.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task<MembershipPlanReadDto> ToggleActiveAsync(int id)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var repo = _unitOfWork.Repository<MembershipPlan>();
        var plan = await repo.FirstOrDefaultAsync(repo.Query().Where(p => p.Id == id));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found.");
        }

        plan.IsActive = !plan.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task SoftDeleteAsync(int id)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var planRepo = _unitOfWork.Repository<MembershipPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(planRepo.Query().Where(p => p.Id == id));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found.");
        }

        var membershipRepo = _unitOfWork.Repository<Membership>();
        var hasActiveMemberships = await membershipRepo.AnyAsync(
            m => m.MembershipPlanId == id && m.Status == MembershipStatus.Active);
        if (hasActiveMemberships)
        {
            throw new AppValidationException("Cannot delete plan with active memberships.");
        }

        plan.IsDeleted = true;
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludingId = null)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var repo = _unitOfWork.Repository<MembershipPlan>();
        var exists = await repo.Query()
            .AsNoTracking()
            .AnyAsync(p => p.Name.ToLower() == normalized && (!excludingId.HasValue || p.Id != excludingId.Value));

        if (exists)
        {
            throw new AppValidationException("Membership plan name already exists.");
        }
    }
}
