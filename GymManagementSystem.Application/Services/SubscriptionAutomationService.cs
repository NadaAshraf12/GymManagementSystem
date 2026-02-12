using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Application.Services;

public class SubscriptionAutomationService : ISubscriptionAutomationService
{
    private const decimal DefaultTrainerCommissionPercent = 10m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;
    private readonly ILogger<SubscriptionAutomationService> _logger;

    public SubscriptionAutomationService(
        IUnitOfWork unitOfWork,
        IInvoicePdfGenerator invoicePdfGenerator,
        ILogger<SubscriptionAutomationService> logger)
    {
        _unitOfWork = unitOfWork;
        _invoicePdfGenerator = invoicePdfGenerator;
        _logger = logger;
    }

    public async Task<SubscriptionAutomationResultDto> ProcessExpirationsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var result = new SubscriptionAutomationResultDto();

        var membershipRepo = _unitOfWork.Repository<Membership>();
        var memberRepo = _unitOfWork.Repository<Member>();
        var walletRepo = _unitOfWork.Repository<WalletTransaction>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var commissionRepo = _unitOfWork.Repository<Commission>();
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var invoiceRepo = _unitOfWork.Repository<Invoice>();

        var expiringSoon = await membershipRepo.ToListAsync(
            membershipRepo.Query()
                .Where(m => m.Status == MembershipStatus.Active && m.EndDate >= utcNow && m.EndDate <= utcNow.AddDays(3)),
            cancellationToken);

        foreach (var item in expiringSoon)
        {
            await notificationRepo.AddAsync(new Notification
            {
                UserId = item.MemberId,
                Title = "Membership Expiring Soon",
                Message = $"Membership #{item.Id} expires on {item.EndDate:yyyy-MM-dd}.",
                IsRead = false
            }, cancellationToken);
        }

        var expiredCandidates = await membershipRepo.ToListAsync(
            membershipRepo.Query()
                .Include(m => m.MembershipPlan)
                .Where(m => m.Status == MembershipStatus.Active && m.EndDate < utcNow)
                .OrderBy(m => m.Id),
            cancellationToken);

        foreach (var membership in expiredCandidates)
        {
            membership.Status = MembershipStatus.Expired;
            membership.UpdatedAt = utcNow;
            result.ExpiredCount++;

            if (!membership.AutoRenewEnabled)
            {
                continue;
            }

            var monthlyPlan = membership.MembershipPlan;
            var walletBalance = await walletRepo.Query()
                .Where(t => t.MemberId == membership.MemberId)
                .Select(t => (decimal?)t.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            if (walletBalance < monthlyPlan.Price)
            {
                result.AutoRenewSkippedCount++;
                _logger.LogInformation(
                    "SubscriptionAutomation AutoRenewSkipped MembershipId={MembershipId} MemberId={MemberId} WalletBalance={WalletBalance}",
                    membership.Id,
                    membership.MemberId,
                    walletBalance);
                continue;
            }

            var renewalStart = utcNow.Date;
            var existingOpenMembership = await membershipRepo.AnyAsync(m =>
                m.MemberId == membership.MemberId &&
                m.Status == MembershipStatus.Active &&
                m.Id != membership.Id);
            if (existingOpenMembership)
            {
                result.AutoRenewSkippedCount++;
                continue;
            }

            var renewal = new Membership
            {
                MemberId = membership.MemberId,
                MembershipPlanId = membership.MembershipPlanId,
                BranchId = membership.BranchId,
                StartDate = renewalStart,
                EndDate = renewalStart.AddDays(monthlyPlan.DurationInDays),
                Status = MembershipStatus.Active,
                Source = membership.Source,
                AutoRenewEnabled = true,
                TotalPaid = 0,
                RemainingBalanceUsedFromWallet = monthlyPlan.Price
            };

            await membershipRepo.AddAsync(renewal, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await walletRepo.AddAsync(new WalletTransaction
            {
                MemberId = membership.MemberId,
                Amount = -monthlyPlan.Price,
                Type = WalletTransactionType.MembershipRenewal,
                ReferenceId = renewal.Id,
                Description = $"Auto-renew debit for membership #{renewal.Id}.",
                CreatedByUserId = null
            }, cancellationToken);

            var assignment = await assignmentRepo.FirstOrDefaultAsync(
                assignmentRepo.Query()
                    .AsNoTracking()
                    .Where(x => x.MemberId == membership.MemberId)
                    .OrderByDescending(x => x.AssignedAt),
                cancellationToken);
            if (assignment != null)
            {
                var commissionAmount = decimal.Round(monthlyPlan.Price * DefaultTrainerCommissionPercent / 100m, 2);
                var renewalCommissionExists = await commissionRepo.AnyAsync(x =>
                    x.MembershipId == renewal.Id &&
                    x.Source == CommissionSource.Renewal);

                if (!renewalCommissionExists)
                {
                    await commissionRepo.AddAsync(new Commission
                    {
                        TrainerId = assignment.TrainerId,
                        MembershipId = renewal.Id,
                        BranchId = renewal.BranchId,
                        Source = CommissionSource.Renewal,
                        Percentage = DefaultTrainerCommissionPercent,
                        CalculatedAmount = commissionAmount,
                        IsPaid = false
                    }, cancellationToken);

                    await notificationRepo.AddAsync(new Notification
                    {
                        UserId = assignment.TrainerId,
                        Title = "Commission Generated",
                        Message = $"Commission {commissionAmount:0.00} generated for membership #{renewal.Id}.",
                        IsRead = false
                    }, cancellationToken);
                }
            }

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32),
                MembershipId = renewal.Id,
                PaymentId = null,
                MemberId = membership.MemberId,
                Amount = monthlyPlan.Price,
                Type = "AutoRenewal",
                FilePath = string.Empty
            };
            await invoiceRepo.AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var invoicePath = await _invoicePdfGenerator.GenerateInvoicePdfAsync(new InvoiceReadDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                MembershipId = invoice.MembershipId,
                MemberId = invoice.MemberId,
                Amount = invoice.Amount,
                Type = invoice.Type,
                FilePath = invoice.FilePath,
                CreatedAt = invoice.CreatedAt
            }, cancellationToken);
            invoice.FilePath = invoicePath;

            await notificationRepo.AddAsync(new Notification
            {
                UserId = membership.MemberId,
                Title = "Auto-Renew Successful",
                Message = $"Membership #{renewal.Id} was auto-renewed successfully.",
                IsRead = false
            }, cancellationToken);

            var member = await memberRepo.FirstOrDefaultAsync(
                memberRepo.Query().Where(m => m.Id == membership.MemberId),
                cancellationToken);
            if (member != null)
            {
                var projectedBalance = walletBalance - monthlyPlan.Price;
                member.WalletBalance = projectedBalance;
            }

            result.AutoRenewedCount++;
            _logger.LogInformation(
                "SubscriptionAutomation AutoRenewed OldMembershipId={OldMembershipId} NewMembershipId={NewMembershipId} MemberId={MemberId}",
                membership.Id,
                renewal.Id,
                membership.MemberId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
