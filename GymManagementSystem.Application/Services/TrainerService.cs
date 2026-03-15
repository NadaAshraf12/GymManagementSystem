using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymManagementSystem.Application.Services;

public class TrainerService : ITrainerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMemoryCache _memoryCache;
    private const string TrainerRole = "Trainer";
    private const string DefaultPassword = "Gym@12345";

    public TrainerService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache memoryCache)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
        _memoryCache = memoryCache;
    }

    public async Task<IReadOnlyList<TrainerReadDto>> GetAllAsync()
    {
        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainers = await trainerRepo.ToListAsync(
            trainerRepo.Query()
                .AsNoTracking()
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName));

        return trainers.Adapt<List<TrainerReadDto>>();
    }

    public async Task<UpdateTrainerDto?> GetByIdAsync(string id)
    {
        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainer = await trainerRepo.FirstOrDefaultAsync(
            trainerRepo.Query().AsNoTracking().Where(t => t.Id == id));
        return trainer == null ? null : trainer.Adapt<UpdateTrainerDto>();
    }

    public async Task<OperationResultDto> CreateAsync(CreateTrainerDto dto)
    {
        if (!await _roleManager.RoleExistsAsync(TrainerRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(TrainerRole));
        }

        var trainer = dto.Adapt<Trainer>();
        trainer.Id = Guid.NewGuid().ToString();
        trainer.Email = dto.Email;
        trainer.UserName = dto.Email;
        trainer.PhoneNumber = dto.PhoneNumber;
        trainer.HireDate = DateTime.UtcNow;
        trainer.IsActive = dto.IsActive;
        trainer.BranchId = dto.BranchId;
        trainer.MustChangePassword = true;

        var createResult = await _userManager.CreateAsync(trainer, DefaultPassword);
        if (!createResult.Succeeded)
        {
            var message = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        await _userManager.AddToRoleAsync(trainer, TrainerRole);
        return OperationResultDto.Ok("Trainer created successfully.");
    }

    public async Task<OperationResultDto> UpdateAsync(UpdateTrainerDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id))
            return OperationResultDto.Fail("Trainer id is required.");

        var trainer = await _userManager.FindByIdAsync(dto.Id) as Trainer;
        if (trainer == null)
            return OperationResultDto.Fail("Trainer not found.");

        dto.Adapt(trainer);
        trainer.Email = dto.Email;
        trainer.UserName = dto.Email;
        trainer.PhoneNumber = dto.PhoneNumber;
        trainer.IsActive = dto.IsActive;
        trainer.BranchId = dto.BranchId;

        var updateResult = await _userManager.UpdateAsync(trainer);
        if (!updateResult.Succeeded)
        {
            var message = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        return OperationResultDto.Ok("Trainer updated successfully.");
    }

    public async Task<OperationResultDto> DeleteAsync(string id)
    {
        var trainer = await _userManager.FindByIdAsync(id) as Trainer;
        if (trainer == null)
            return OperationResultDto.Fail("Trainer not found.");

        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var hasAssignments = await assignmentRepo.AnyAsync(a => a.TrainerId == id);
        if (hasAssignments)
            return OperationResultDto.Fail("Trainer has active assignments. Remove members first.");

        var deleteResult = await _userManager.DeleteAsync(trainer);
        if (!deleteResult.Succeeded)
        {
            var message = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        return OperationResultDto.Ok("Trainer deleted successfully.");
    }

    public async Task<TrainerFinancialProfileDto> GetTrainerFinancialProfileAsync(string trainerId)
    {
        if (string.IsNullOrWhiteSpace(trainerId))
        {
            throw new ArgumentException("Trainer id is required.", nameof(trainerId));
        }

        var cacheKey = $"trainer-financial-profile:{trainerId}";
        if (_memoryCache.TryGetValue(cacheKey, out TrainerFinancialProfileDto? cached) && cached != null)
        {
            return cached;
        }

        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainerInfo = await trainerRepo.Query()
            .AsNoTracking()
            .Where(t => t.Id == trainerId)
            .Select(t => new
            {
                TrainerName = t.FirstName + " " + t.LastName,
                BranchName = t.Branch != null ? t.Branch.Name : "Unassigned"
            })
            .FirstOrDefaultAsync();

        if (trainerInfo == null)
        {
            throw new KeyNotFoundException("Trainer not found.");
        }

        var commissionRepo = _unitOfWork.Repository<Commission>();
        var commissionBaseQuery = commissionRepo.Query()
            .AsNoTracking()
            .Where(c => c.TrainerId == trainerId);

        var commissionTotals = await commissionBaseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalGenerated = g.Sum(x => x.CalculatedAmount),
                TotalPaid = g.Where(x => x.IsPaid).Sum(x => x.CalculatedAmount),
                TotalPending = g.Where(x => !x.IsPaid).Sum(x => x.CalculatedAmount)
            })
            .FirstOrDefaultAsync();

        var commissions = await commissionBaseQuery
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TrainerCommissionFinancialRowDto
            {
                Amount = c.CalculatedAmount,
                Source = c.Source.ToString(),
                Status = c.IsPaid ? "Paid" : "Generated",
                MembershipPlanName = c.Membership.MembershipPlan.Name,
                MemberName = c.Membership.Member.FirstName + " " + c.Membership.Member.LastName,
                Date = c.CreatedAt
            })
            .Take(100)
            .ToListAsync();

        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var membershipRepo = _unitOfWork.Repository<Membership>();
        var membershipRevenues = await (
            from assignment in assignmentRepo.Query().AsNoTracking()
            where assignment.TrainerId == trainerId
            join membership in membershipRepo.Query().AsNoTracking()
                on assignment.MemberId equals membership.MemberId
            where membership.Status == MembershipStatus.Active && !membership.IsDeleted
            orderby membership.StartDate descending
            select new TrainerMembershipRevenueRowDto
            {
                MemberName = membership.Member.FirstName + " " + membership.Member.LastName,
                PlanName = membership.MembershipPlan.Name,
                RevenueAmount = membership.TotalPaid,
                StartDate = membership.StartDate
            })
            .ToListAsync();

        var membershipRevenueFromTrainerMembers = membershipRevenues.Sum(x => x.RevenueAmount);

        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();
        var sessionEarnings = await memberSessionRepo.Query()
            .AsNoTracking()
            .Where(ms => ms.WorkoutSession.TrainerId == trainerId && ms.ChargedPrice > 0)
            .OrderByDescending(ms => ms.BookingDate)
            .Select(ms => new TrainerSessionEarningRowDto
            {
                SessionTitle = ms.WorkoutSession.Title,
                MemberName = ms.Member.FirstName + " " + ms.Member.LastName,
                Price = ms.ChargedPrice,
                TrainerShare = ms.ChargedPrice,
                Date = ms.BookingDate
            })
            .Take(100)
            .ToListAsync();

        var sessionRevenue = sessionEarnings.Sum(x => x.TrainerShare);

        var startDate = DateTime.UtcNow.Date.AddDays(-29);
        var rawTrend = await commissionBaseQuery
            .Where(c => c.CreatedAt.Date >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.CalculatedAmount) })
            .ToListAsync();

        var trendLookup = rawTrend.ToDictionary(x => x.Date, x => x.Amount);
        var commissionLast30Days = Enumerable.Range(0, 30)
            .Select(i => startDate.AddDays(i))
            .Select(day => new TrainerCommissionTrendPointDto
            {
                Date = day,
                Amount = trendLookup.TryGetValue(day, out var amount) ? amount : 0m
            })
            .ToList();

        var recentTransactions = commissions
            .Select(c => new TrainerFinancialTransactionDto
            {
                Type = "Commission",
                Description = $"{c.Source} - {c.MembershipPlanName} ({c.MemberName})",
                Amount = c.Amount,
                Status = c.Status,
                Date = c.Date
            })
            .Concat(membershipRevenues.Select(m => new TrainerFinancialTransactionDto
            {
                Type = "Membership",
                Description = $"{m.PlanName} ({m.MemberName})",
                Amount = m.RevenueAmount,
                Status = "Active",
                Date = m.StartDate
            }))
            .Concat(sessionEarnings.Select(s => new TrainerFinancialTransactionDto
            {
                Type = "Session",
                Description = $"{s.SessionTitle} ({s.MemberName})",
                Amount = s.TrainerShare,
                Status = "Paid",
                Date = s.Date
            }))
            .OrderByDescending(x => x.Date)
            .Take(20)
            .ToList();

        var profile = new TrainerFinancialProfileDto
        {
            TrainerName = trainerInfo.TrainerName,
            BranchName = trainerInfo.BranchName,
            TotalGeneratedCommission = commissionTotals?.TotalGenerated ?? 0m,
            TotalPaidCommission = commissionTotals?.TotalPaid ?? 0m,
            TotalPendingCommission = commissionTotals?.TotalPending ?? 0m,
            MembershipRevenueFromTrainerMembers = membershipRevenueFromTrainerMembers,
            SessionRevenue = sessionRevenue,
            Commissions = commissions,
            MembershipRevenues = membershipRevenues,
            SessionEarnings = sessionEarnings,
            RecentTransactions = recentTransactions,
            CommissionLast30Days = commissionLast30Days
        };

        _memoryCache.Set(
            cacheKey,
            profile,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

        return profile;
    }
}

