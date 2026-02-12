using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Application.Services;

public class RevenueMetricsService : IRevenueMetricsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RevenueMetricsService> _logger;

    public RevenueMetricsService(
        IUnitOfWork unitOfWork,
        IMemoryCache memoryCache,
        ILogger<RevenueMetricsService> logger)
    {
        _unitOfWork = unitOfWork;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<int> GetActiveMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Membership>();
        var query = repo.Query().Where(m => m.Status == MembershipStatus.Active);
        query = query.AsNoTracking();
        if (branchId.HasValue)
        {
            query = query.Where(m => m.BranchId == branchId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetExpiringSoonMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Membership>();
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(7);
        var query = repo.Query()
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate >= now && m.EndDate <= horizon);
        query = query.AsNoTracking();
        if (branchId.HasValue)
        {
            query = query.Where(m => m.BranchId == branchId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetExpiredMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Membership>();
        var query = repo.Query().Where(m => m.Status == MembershipStatus.Expired);
        query = query.AsNoTracking();
        if (branchId.HasValue)
        {
            query = query.Where(m => m.BranchId == branchId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Membership>();
        var query = repo.Query()
            .AsNoTracking()
            .Include(m => m.MembershipPlan)
            .Where(m => m.Status == MembershipStatus.Active);
        if (branchId.HasValue)
        {
            query = query.Where(m => m.BranchId == branchId.Value);
        }

        var activeMemberships = await repo.ToListAsync(
            query,
            cancellationToken);

        return activeMemberships.Sum(m => NormalizeToMonthly(m.MembershipPlan));
    }

    public async Task<decimal> GetTotalRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var sessionRevenue = await GetTotalSessionRevenueAsync(branchId, cancellationToken);
        var membershipRevenue = await GetTotalMembershipRevenueAsync(branchId, cancellationToken);
        var addOnRevenue = await GetTotalAddOnRevenueAsync(branchId, cancellationToken);
        return sessionRevenue + membershipRevenue + addOnRevenue;
    }

    public async Task<decimal> GetTotalSessionRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<WalletTransaction>();
        var query = repo.Query()
            .AsNoTracking()
            .Include(w => w.Member)
            .Where(w => w.Type == WalletTransactionType.SessionBooking)
            .AsQueryable();
        if (branchId.HasValue)
        {
            query = query.Where(w => w.Member.BranchId == branchId.Value);
        }

        var total = await query.Select(w => (decimal?)w.Amount).SumAsync(cancellationToken);
        return Math.Abs(total ?? 0m);
    }

    public async Task<decimal> GetTotalMembershipRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var paymentRepo = _unitOfWork.Repository<Payment>();
        var paymentQuery = paymentRepo.Query()
            .AsNoTracking()
            .Include(p => p.Membership)
            .Where(p => p.PaymentStatus == PaymentStatus.Confirmed)
            .Where(p => !p.Membership.IsDeleted);
        if (branchId.HasValue)
        {
            paymentQuery = paymentQuery.Where(p => p.Membership.BranchId == branchId.Value);
        }
        var paymentsTotal = await paymentQuery.Select(p => (decimal?)p.Amount).SumAsync(cancellationToken) ?? 0m;

        var walletRepo = _unitOfWork.Repository<WalletTransaction>();
        var walletQuery = walletRepo.Query()
            .AsNoTracking()
            .Include(w => w.Member)
            .Where(w => w.Type == WalletTransactionType.MembershipRenewal || w.Type == WalletTransactionType.MembershipUpgrade)
            .AsQueryable();
        if (branchId.HasValue)
        {
            walletQuery = walletQuery.Where(w => w.Member.BranchId == branchId.Value);
        }
        var walletTotal = await walletQuery.Select(w => (decimal?)w.Amount).SumAsync(cancellationToken) ?? 0m;

        return paymentsTotal + Math.Abs(walletTotal);
    }

    public async Task<decimal> GetTotalAddOnRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<WalletTransaction>();
        var query = repo.Query()
            .AsNoTracking()
            .Include(w => w.Member)
            .Where(w => w.Type == WalletTransactionType.AddOnPurchase)
            .AsQueryable();
        if (branchId.HasValue)
        {
            query = query.Where(w => w.Member.BranchId == branchId.Value);
        }

        var total = await query.Select(w => (decimal?)w.Amount).SumAsync(cancellationToken);
        return Math.Abs(total ?? 0m);
    }

    public async Task<decimal> GetWalletTotalBalanceAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<WalletTransaction>();
        var query = repo.Query()
            .AsNoTracking()
            .Include(w => w.Member)
            .Where(w => !w.Member.IsDeleted)
            .AsQueryable();
        if (branchId.HasValue)
        {
            query = query.Where(w => w.Member.BranchId == branchId.Value);
        }

        var total = await query.Select(w => (decimal?)w.Amount).SumAsync(cancellationToken);

        return total ?? 0m;
    }

    public async Task<RevenueMetricsDto> GetDashboardMetricsAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var version = await GetMetricsVersionAsync(branchId, cancellationToken);
        var cacheKey = $"dashboard-metrics:{branchId?.ToString() ?? "all"}:{version}";
        if (_memoryCache.TryGetValue(cacheKey, out RevenueMetricsDto? cached) && cached != null)
        {
            return cached;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var active = await GetActiveMembershipCountAsync(branchId, cancellationToken);
        var expired = await GetExpiredMembershipCountAsync(branchId, cancellationToken);
        var expiringSoon = await GetExpiringSoonMembershipCountAsync(branchId, cancellationToken);
        var totalSessionRevenue = await GetTotalSessionRevenueAsync(branchId, cancellationToken);
        var totalMembershipRevenue = await GetTotalMembershipRevenueAsync(branchId, cancellationToken);
        var totalAddOnRevenue = await GetTotalAddOnRevenueAsync(branchId, cancellationToken);
        var totalRevenue = totalSessionRevenue + totalMembershipRevenue + totalAddOnRevenue;
        var mrr = await GetMonthlyRecurringRevenueAsync(branchId, cancellationToken);
        var totalWallet = await GetWalletTotalBalanceAsync(branchId, cancellationToken);

        var commissionRepo = _unitOfWork.Repository<Commission>();
        var commissionQuery = commissionRepo.Query()
            .AsNoTracking()
            .Include(c => c.Membership)
            .AsQueryable();
        if (branchId.HasValue)
        {
            commissionQuery = commissionQuery.Where(c => c.Membership.BranchId == branchId.Value);
        }

        var commissionsOwed = await commissionQuery
            .Where(c => !c.IsPaid)
            .Select(c => (decimal?)c.CalculatedAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var commissionsPaid = await commissionQuery
            .Where(c => c.IsPaid)
            .Select(c => (decimal?)c.CalculatedAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var walletRepo = _unitOfWork.Repository<WalletTransaction>();
        var walletQuery = walletRepo.Query()
            .AsNoTracking()
            .Include(w => w.Member)
            .AsQueryable();
        if (branchId.HasValue)
        {
            walletQuery = walletQuery.Where(w => w.Member.BranchId == branchId.Value);
        }

        var walletCredits = await walletQuery
            .Where(w => w.Amount > 0)
            .Select(w => (decimal?)w.Amount)
            .SumAsync(cancellationToken) ?? 0m;
        var walletDebitsRaw = await walletQuery
            .Where(w => w.Amount < 0)
            .Select(w => (decimal?)w.Amount)
            .SumAsync(cancellationToken) ?? 0m;
        var walletDebits = Math.Abs(walletDebitsRaw);

        var result = new RevenueMetricsDto
        {
            BranchId = branchId,
            ActiveMemberships = active,
            ExpiringSoonMemberships = expiringSoon,
            ExpiredMemberships = expired,
            TotalRevenue = totalRevenue,
            TotalSessionRevenue = totalSessionRevenue,
            TotalMembershipRevenue = totalMembershipRevenue,
            TotalAddOnRevenue = totalAddOnRevenue,
            MonthlyRecurringRevenue = mrr,
            TotalWalletBalance = totalWallet,
            TotalCommissionsOwed = commissionsOwed,
            TotalCommissionsPaid = commissionsPaid,
            WalletTotalCredits = walletCredits,
            WalletTotalDebits = walletDebits
        };

        _memoryCache.Set(
            cacheKey,
            result,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

        stopwatch.Stop();
        _logger.LogInformation(
            "Revenue metrics calculated. BranchId={BranchId} ElapsedMs={ElapsedMs}",
            branchId,
            stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<IReadOnlyList<RevenueByBranchTypeDto>> GetRevenuePerBranchByTypeAsync(CancellationToken cancellationToken = default)
    {
        var branchRepo = _unitOfWork.Repository<Branch>();
        var branches = await branchRepo.ToListAsync(branchRepo.Query().AsNoTracking().OrderBy(b => b.Name), cancellationToken);

        var result = new List<RevenueByBranchTypeDto>();
        foreach (var branch in branches)
        {
            var membershipRevenue = await GetTotalMembershipRevenueAsync(branch.Id, cancellationToken);
            var sessionRevenue = await GetTotalSessionRevenueAsync(branch.Id, cancellationToken);
            var addOnRevenue = await GetTotalAddOnRevenueAsync(branch.Id, cancellationToken);
            result.Add(new RevenueByBranchTypeDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                MembershipRevenue = membershipRevenue,
                SessionRevenue = sessionRevenue,
                AddOnRevenue = addOnRevenue
            });
        }

        return result;
    }

    private static decimal NormalizeToMonthly(MembershipPlan plan)
    {
        if (plan.DurationInDays >= 360)
        {
            return decimal.Round(plan.Price / 12m, 2);
        }

        if (plan.DurationInDays <= 31)
        {
            return plan.Price;
        }

        return decimal.Round(plan.Price * 30m / plan.DurationInDays, 2);
    }

    private async Task<long> GetMetricsVersionAsync(int? branchId, CancellationToken cancellationToken)
    {
        var membershipRepo = _unitOfWork.Repository<Membership>();
        var walletRepo = _unitOfWork.Repository<WalletTransaction>();
        var paymentRepo = _unitOfWork.Repository<Payment>();
        var commissionRepo = _unitOfWork.Repository<Commission>();
        var invoiceRepo = _unitOfWork.Repository<Invoice>();

        var maxMembershipUpdatedAt = await membershipRepo.Query()
            .AsNoTracking()
            .Where(m => !branchId.HasValue || m.BranchId == branchId.Value)
            .Select(m => (DateTime?)(m.UpdatedAt ?? m.CreatedAt))
            .MaxAsync(cancellationToken);

        var maxWalletCreatedAt = await walletRepo.Query()
            .AsNoTracking()
            .Where(w => !branchId.HasValue || w.Member.BranchId == branchId.Value)
            .Select(w => (DateTime?)w.CreatedAt)
            .MaxAsync(cancellationToken);

        var maxPaymentUpdatedAt = await paymentRepo.Query()
            .AsNoTracking()
            .Where(p => !branchId.HasValue || p.Membership.BranchId == branchId.Value)
            .Select(p => (DateTime?)(p.UpdatedAt ?? p.CreatedAt))
            .MaxAsync(cancellationToken);

        var maxCommissionUpdatedAt = await commissionRepo.Query()
            .AsNoTracking()
            .Where(c => !branchId.HasValue || c.Membership.BranchId == branchId.Value)
            .Select(c => (DateTime?)(c.UpdatedAt ?? c.CreatedAt))
            .MaxAsync(cancellationToken);

        var maxInvoiceUpdatedAt = await invoiceRepo.Query()
            .AsNoTracking()
            .Where(i => !branchId.HasValue
                || (i.Membership != null && i.Membership.BranchId == branchId.Value)
                || (i.AddOn != null && i.AddOn.BranchId == branchId.Value))
            .Select(i => (DateTime?)(i.UpdatedAt ?? i.CreatedAt))
            .MaxAsync(cancellationToken);

        var maxMembershipUpdated = maxMembershipUpdatedAt?.Ticks ?? 0L;
        var maxWalletCreated = maxWalletCreatedAt?.Ticks ?? 0L;
        var maxPaymentUpdated = maxPaymentUpdatedAt?.Ticks ?? 0L;
        var maxCommissionUpdated = maxCommissionUpdatedAt?.Ticks ?? 0L;
        var maxInvoiceUpdated = maxInvoiceUpdatedAt?.Ticks ?? 0L;

        return Math.Max(
            Math.Max(maxMembershipUpdated, maxWalletCreated),
            Math.Max(Math.Max(maxPaymentUpdated, maxCommissionUpdated), maxInvoiceUpdated));
    }
}
