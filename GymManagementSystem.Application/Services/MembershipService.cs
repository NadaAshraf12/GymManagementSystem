using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.Application.Services;

public class MembershipService : IMembershipService
{
    private const decimal DefaultTrainerCommissionPercent = 10m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly IEnumerable<IPaymentGateway> _paymentGateways;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAppAuthorizationService authorizationService,
        IEnumerable<IPaymentGateway> paymentGateways,
        IInvoicePdfGenerator invoicePdfGenerator,
        ILogger<MembershipService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _paymentGateways = paymentGateways;
        _invoicePdfGenerator = invoicePdfGenerator;
        _logger = logger;
    }

    public async Task<MembershipPlanReadDto> CreateMembershipPlanAsync(CreateMembershipPlanDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var planRepo = _unitOfWork.Repository<MembershipPlan>();
        var plan = dto.Adapt<MembershipPlan>();
        await planRepo.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task<MembershipPlanReadDto> UpdateMembershipPlanAsync(UpdateMembershipPlanDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var planRepo = _unitOfWork.Repository<MembershipPlan>();
        var plan = await planRepo.FirstOrDefaultAsync(planRepo.Query().Where(p => p.Id == dto.Id));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found.");
        }

        dto.Adapt(plan);
        plan.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return plan.Adapt<MembershipPlanReadDto>();
    }

    public async Task<IReadOnlyList<MembershipPlanReadDto>> GetMembershipPlansAsync(bool activeOnly)
    {
        var planRepo = _unitOfWork.Repository<MembershipPlan>();
        var query = planRepo.Query().AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        var plans = await planRepo.ToListAsync(query.OrderBy(p => p.Price));
        return plans.Adapt<List<MembershipPlanReadDto>>();
    }

    public async Task<MembershipReadDto> CreateMembershipAsync(CreateMembershipDto dto)
    {
        var source = dto.Source == MembershipSource.InGym
            ? MembershipCreationSource.Admin
            : MembershipCreationSource.MemberPortal;
        var paymentMethod = NormalizePaymentMethod(source, dto.PaymentMethod, dto.WalletAmountToUse);

        return await CreateMembershipAsync(new CreateMembershipCommand
        {
            MemberId = dto.MemberId,
            PlanId = dto.MembershipPlanId,
            Source = source,
            PaymentMethod = paymentMethod,
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            AutoRenewEnabled = dto.AutoRenewEnabled,
            PaymentAmountOverride = dto.PaymentAmount
        });
    }

    public async Task<MembershipReadDto> RequestSubscriptionAsync(RequestSubscriptionDto dto)
    {
        var paymentMethod = NormalizePaymentMethod(MembershipCreationSource.MemberPortal, dto.PaymentMethod, dto.WalletAmountToUse);
        return await CreateMembershipAsync(new CreateMembershipCommand
        {
            MemberId = dto.MemberId,
            PlanId = dto.MembershipPlanId,
            Source = MembershipCreationSource.MemberPortal,
            PaymentMethod = paymentMethod,
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            AutoRenewEnabled = dto.AutoRenewEnabled,
            PaymentAmountOverride = dto.PaymentAmount
        });
    }

    public async Task<MembershipReadDto> ActivatePendingMembershipAsync(int membershipId)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var membershipRepo = _unitOfWork.Repository<Membership>();
            var membership = await membershipRepo.FirstOrDefaultAsync(
                membershipRepo.Query()
                    .Include(m => m.MembershipPlan)
                    .Include(m => m.Member)
                    .Include(m => m.Payments)
                    .Where(m => m.Id == membershipId));

            if (membership == null)
            {
                throw new NotFoundException("Membership not found.");
            }

            if (membership.Status != MembershipStatus.PendingPayment)
            {
                throw new AppValidationException("Only pending memberships can be activated.");
            }

            var pendingPayment = membership.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();
            if (pendingPayment == null)
            {
                throw new AppValidationException("Pending payment not found for membership.");
            }

            await ActivatePendingMembershipInternalAsync(membership, pendingPayment);

            await _unitOfWork.SaveChangesAsync();
            await SyncWalletProjectionAsync(membership.Member);
            await _unitOfWork.SaveChangesAsync();

            return await GetMembershipWithPaymentsAsync(membership.Id);
        });
    }

    public async Task<MembershipReadDto> CreateDirectMembershipAsync(CreateDirectMembershipDto dto)
    {
        var paymentMethod = NormalizePaymentMethod(MembershipCreationSource.Admin, dto.PaymentMethod, dto.WalletAmountToUse);
        return await CreateMembershipAsync(new CreateMembershipCommand
        {
            MemberId = dto.MemberId,
            PlanId = dto.MembershipPlanId,
            Source = MembershipCreationSource.Admin,
            PaymentMethod = paymentMethod,
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            AutoRenewEnabled = dto.AutoRenewEnabled,
            PaymentAmountOverride = dto.PaymentAmount
        });
    }

    public async Task<MembershipReadDto> CreateMembershipAsync(CreateMembershipCommand command)
    {
        if (command.Source == MembershipCreationSource.Admin)
        {
            await _authorizationService.EnsureAdminFullAccessAsync();
        }
        else
        {
            await EnsureCanRequestSubscriptionAsync(command.MemberId);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (member, plan, _) = await ValidateAndPrepareMembershipContextAsync(
                command.MemberId,
                command.PlanId,
                command.BranchId,
                0m);

            await EnsureNoCommerciallyOpenMembershipAsync(command.MemberId);

            var method = NormalizePaymentMethod(command.Source, command.PaymentMethod, 0m);
            var effectivePrice = CalculateEffectivePrice(plan);
            var isPaid = method is PaymentMethod.Cash or PaymentMethod.Wallet;
            var paymentAmount = method == PaymentMethod.Wallet
                ? effectivePrice
                : (command.PaymentAmountOverride ?? effectivePrice);

            if (command.Source == MembershipCreationSource.Admin && method == PaymentMethod.Proof)
            {
                throw new AppValidationException("Admin creation supports only Cash or Wallet.");
            }

            if (command.Source == MembershipCreationSource.MemberPortal && method == PaymentMethod.Cash)
            {
                throw new AppValidationException("Portal subscription does not allow Cash.");
            }

            if (method == PaymentMethod.Wallet)
            {
                var balance = await CalculateWalletBalanceAsync(member.Id);
                if (balance < effectivePrice)
                {
                    throw new AppValidationException("Insufficient wallet balance.");
                }
            }

            if (isPaid && paymentAmount < effectivePrice)
            {
                throw new AppValidationException("Payment is insufficient for this membership plan.");
            }

            var membership = new Membership
            {
                MemberId = command.MemberId,
                BranchId = command.BranchId ?? member.BranchId,
                MembershipPlanId = command.PlanId,
                StartDate = command.StartDate.Date,
                EndDate = command.StartDate.Date.AddDays(plan.DurationInDays),
                Source = command.Source == MembershipCreationSource.Admin ? MembershipSource.InGym : MembershipSource.Online,
                Status = isPaid ? MembershipStatus.Active : MembershipStatus.PendingPayment,
                AutoRenewEnabled = command.AutoRenewEnabled,
                TotalPaid = 0,
                RemainingBalanceUsedFromWallet = 0
            };

            var membershipRepo = _unitOfWork.Repository<Membership>();
            await membershipRepo.AddAsync(membership);
            await _unitOfWork.SaveChangesAsync();

            if (method == PaymentMethod.Wallet)
            {
                await AddWalletTransactionAsync(
                    command.MemberId,
                    -effectivePrice,
                    WalletTransactionType.MembershipRenewal,
                    membership.Id,
                    $"Wallet debit for membership #{membership.Id}.");
            }

            var payment = new Payment
            {
                MembershipId = membership.Id,
                Amount = paymentAmount,
                PaymentMethod = method,
                PaymentStatus = isPaid ? PaymentStatus.Paid : PaymentStatus.Pending,
                PaidAt = isPaid ? DateTime.UtcNow : null,
                ReviewedAt = isPaid ? DateTime.UtcNow : null,
                ConfirmedByAdminId = command.Source == MembershipCreationSource.Admin ? _currentUserService.UserId : null
            };

            var paymentRepo = _unitOfWork.Repository<Payment>();
            await paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            if (isPaid)
            {
                await ApplyConfirmedPaymentAsync(membership, plan, payment);
            }

            await _unitOfWork.SaveChangesAsync();
            await SyncWalletProjectionAsync(member);
            await _unitOfWork.SaveChangesAsync();

            return await GetMembershipWithPaymentsAsync(membership.Id);
        });
    }

    public async Task<MembershipReadDto> UpgradeMembershipAsync(UpgradeMembershipDto dto)
    {
        await EnsureCanAccessMemberAsync(dto.MemberId);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var membershipRepo = _unitOfWork.Repository<Membership>();
            var planRepo = _unitOfWork.Repository<MembershipPlan>();
            var memberRepo = _unitOfWork.Repository<Member>();

            var currentMembership = await membershipRepo.FirstOrDefaultAsync(
                membershipRepo.Query()
                    .Include(m => m.MembershipPlan)
                    .Where(m => m.MemberId == dto.MemberId && m.Status == MembershipStatus.Active)
                    .OrderByDescending(m => m.EndDate));
            if (currentMembership == null)
            {
                throw new NotFoundException("No active membership found to upgrade.");
            }

            var activeMembershipCount = await membershipRepo.Query()
                .Where(m => m.MemberId == dto.MemberId && m.Status == MembershipStatus.Active)
                .CountAsync();
            if (activeMembershipCount > 1)
            {
                throw new AppValidationException("Member has inconsistent multiple active memberships.");
            }

            var newPlan = await planRepo.FirstOrDefaultAsync(
                planRepo.Query().Where(p => p.Id == dto.NewMembershipPlanId && p.IsActive));
            if (newPlan == null)
            {
                throw new NotFoundException("Target membership plan not found.");
            }

            if (newPlan.Price <= currentMembership.MembershipPlan.Price)
            {
                throw new AppValidationException("Target plan must be higher than current plan.");
            }

            var difference = newPlan.Price - currentMembership.MembershipPlan.Price;
            var balance = await CalculateWalletBalanceAsync(dto.MemberId);
            if (balance < difference)
            {
                throw new AppValidationException("Insufficient wallet balance for upgrade.");
            }

            currentMembership.Status = MembershipStatus.Cancelled;
            currentMembership.UpdatedAt = DateTime.UtcNow;

            var upgraded = new Membership
            {
                MemberId = dto.MemberId,
                BranchId = currentMembership.BranchId,
                MembershipPlanId = newPlan.Id,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(newPlan.DurationInDays),
                Status = MembershipStatus.Active,
                Source = currentMembership.Source,
                AutoRenewEnabled = currentMembership.AutoRenewEnabled,
                TotalPaid = 0,
                RemainingBalanceUsedFromWallet = difference
            };
            await membershipRepo.AddAsync(upgraded);
            await _unitOfWork.SaveChangesAsync();

            await AddWalletTransactionAsync(
                dto.MemberId,
                -difference,
                WalletTransactionType.MembershipUpgrade,
                upgraded.Id,
                $"Wallet debit for membership upgrade to plan #{newPlan.Id}.");

            await CreateInvoiceAsync(upgraded, difference, "MembershipUpgrade", null);
            await AddNotificationAsync(
                dto.MemberId,
                "Membership Upgraded",
                $"Your membership was upgraded successfully. Debited {difference:0.00} from wallet.");

            await _unitOfWork.SaveChangesAsync();

            var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == dto.MemberId));
            if (member != null)
            {
                member.WalletBalance = await CalculateWalletBalanceAsync(dto.MemberId);
                await _unitOfWork.SaveChangesAsync();
            }

            return await GetMembershipWithPaymentsAsync(upgraded.Id);
        });
    }

    public async Task<MembershipReadDto> ConfirmPaymentAsync(int paymentId, ConfirmPaymentDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var paymentRepo = _unitOfWork.Repository<Payment>();
        var payment = await paymentRepo.FirstOrDefaultAsync(
            paymentRepo.Query()
                .Include(p => p.Membership)
                .ThenInclude(m => m.MembershipPlan)
                .Include(p => p.Membership)
                .ThenInclude(m => m.Member)
                .Where(p => p.Id == paymentId));

        if (payment == null)
        {
            throw new NotFoundException("Payment not found.");
        }

        if (payment.PaymentStatus != PaymentStatus.Pending)
        {
            throw new AppValidationException("Only pending payments can be confirmed.");
        }

        if (dto.ConfirmedAmount.HasValue)
        {
            payment.Amount = dto.ConfirmedAmount.Value;
        }

        var membership = payment.Membership;
        await ActivatePendingMembershipInternalAsync(membership, payment);
        await _unitOfWork.SaveChangesAsync();
        await SyncWalletProjectionAsync(membership.Member);
        await _unitOfWork.SaveChangesAsync();
        return await GetMembershipWithPaymentsAsync(membership.Id);
    }

    public async Task<MembershipReadDto> RejectPaymentAsync(int paymentId)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var paymentRepo = _unitOfWork.Repository<Payment>();
        var payment = await paymentRepo.FirstOrDefaultAsync(
            paymentRepo.Query()
                .Include(p => p.Membership)
                .ThenInclude(m => m.Member)
                .Where(p => p.Id == paymentId));

        if (payment == null)
        {
            throw new NotFoundException("Payment not found.");
        }

        if (payment.PaymentStatus != PaymentStatus.Pending)
        {
            throw new AppValidationException("Only pending payments can be rejected.");
        }

        var membership = payment.Membership;
        var member = membership.Member;

        payment.PaymentStatus = PaymentStatus.Rejected;
        payment.RejectionReason = "Rejected by admin.";
        payment.ReviewedAt = DateTime.UtcNow;
        payment.ConfirmedByAdminId = _currentUserService.UserId;
        await AddNotificationAsync(
            membership.MemberId,
            "Payment Rejected",
            $"Payment for membership #{membership.Id} was rejected.");

        if (membership.RemainingBalanceUsedFromWallet > 0)
        {
            await AddWalletTransactionAsync(
                member.Id,
                membership.RemainingBalanceUsedFromWallet,
                WalletTransactionType.Refund,
                membership.Id,
                $"Wallet refund for rejected membership #{membership.Id}.");

            membership.RemainingBalanceUsedFromWallet = 0;
        }

        membership.Status = MembershipStatus.PendingPayment;
        membership.TotalPaid = 0;

        await _unitOfWork.SaveChangesAsync();
        await SyncWalletProjectionAsync(member);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Payment {PaymentId} rejected for membership {MembershipId} by user {UserId}.",
            payment.Id,
            membership.Id,
            _currentUserService.UserId ?? "system");

        return await GetMembershipWithPaymentsAsync(membership.Id);
    }

    public async Task<PaymentReadDto> UploadPaymentProofAsync(UploadPaymentProofDto dto)
    {
        var paymentRepo = _unitOfWork.Repository<Payment>();
        var payment = await paymentRepo.FirstOrDefaultAsync(
            paymentRepo.Query()
                .Include(p => p.Membership)
                .Where(p => p.Id == dto.PaymentId));

        if (payment == null)
        {
            throw new NotFoundException("Payment not found.");
        }

        await _authorizationService.EnsureMemberOwnsResourceAsync(payment.Membership.MemberId);

        if (payment.PaymentStatus != PaymentStatus.Pending)
        {
            throw new AppValidationException("Proof can only be uploaded for pending payments.");
        }

        payment.PaymentProofUrl = dto.PaymentProofUrl;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return payment.Adapt<PaymentReadDto>();
    }

    public async Task<IReadOnlyList<PendingPaymentReadDto>> GetPendingPaymentsAsync()
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var paymentRepo = _unitOfWork.Repository<Payment>();
        var pending = await paymentRepo.ToListAsync(
            paymentRepo.Query()
                .AsNoTracking()
                .Include(p => p.Membership)
                .Where(p => p.PaymentStatus == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt));

        return pending.Adapt<List<PendingPaymentReadDto>>();
    }

    public async Task<MembershipReadDto> ReviewPaymentAsync(int paymentId, ReviewPaymentDto dto)
    {
        if (dto.Approve)
        {
            return await ConfirmPaymentAsync(paymentId, new ConfirmPaymentDto
            {
                ConfirmedAmount = dto.ConfirmedAmount
            });
        }

        if (string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new AppValidationException("Rejection reason is required.");
        }

        await _authorizationService.EnsureAdminFullAccessAsync();

        var paymentRepo = _unitOfWork.Repository<Payment>();
        var payment = await paymentRepo.FirstOrDefaultAsync(
            paymentRepo.Query()
                .Include(p => p.Membership)
                .ThenInclude(m => m.Member)
                .Where(p => p.Id == paymentId));

        if (payment == null)
        {
            throw new NotFoundException("Payment not found.");
        }

        if (payment.PaymentStatus != PaymentStatus.Pending)
        {
            throw new AppValidationException("Only pending payments can be reviewed.");
        }

        payment.PaymentStatus = PaymentStatus.Rejected;
        payment.RejectionReason = dto.RejectionReason;
        payment.ReviewedAt = DateTime.UtcNow;
        payment.ConfirmedByAdminId = _currentUserService.UserId;

        var membership = payment.Membership;
        membership.Status = MembershipStatus.PendingPayment;
        await AddNotificationAsync(
            membership.MemberId,
            "Payment Rejected",
            $"Payment for membership #{membership.Id} was rejected. Reason: {dto.RejectionReason}");

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Payment {PaymentId} rejected with reason by user {UserId}. Membership {MembershipId} remains {Status}.",
            paymentId,
            _currentUserService.UserId ?? "system",
            membership.Id,
            membership.Status);

        return await GetMembershipWithPaymentsAsync(membership.Id);
    }

    public async Task<MembershipReadDto> FreezeMembershipAsync(int membershipId, FreezeMembershipDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var membershipRepo = _unitOfWork.Repository<Membership>();
        var membership = await membershipRepo.FirstOrDefaultAsync(
            membershipRepo.Query().Where(m => m.Id == membershipId));

        if (membership == null)
        {
            throw new NotFoundException("Membership not found.");
        }

        if (membership.Status != MembershipStatus.Active)
        {
            throw new AppValidationException("Only active memberships can be frozen.");
        }

        membership.Status = MembershipStatus.Frozen;
        membership.FreezeStartDate = dto.FreezeStartDate;
        membership.FreezeEndDate = null;
        membership.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Membership {MembershipId} frozen by user {UserId} at {FreezeStartDate}.",
            membershipId,
            _currentUserService.UserId ?? "system",
            dto.FreezeStartDate);

        return membership.Adapt<MembershipReadDto>();
    }

    public async Task<MembershipReadDto> ResumeMembershipAsync(int membershipId)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        var membershipRepo = _unitOfWork.Repository<Membership>();
        var membership = await membershipRepo.FirstOrDefaultAsync(
            membershipRepo.Query().Where(m => m.Id == membershipId));

        if (membership == null)
        {
            throw new NotFoundException("Membership not found.");
        }

        if (membership.Status != MembershipStatus.Frozen || !membership.FreezeStartDate.HasValue)
        {
            throw new AppValidationException("Membership is not frozen.");
        }

        await EnsureNoCommerciallyOpenMembershipAsync(membership.MemberId, membership.Id);

        var now = DateTime.UtcNow;
        var freezeStart = membership.FreezeStartDate.Value;
        var freezeDuration = now - freezeStart;
        if (freezeDuration < TimeSpan.Zero)
        {
            freezeDuration = TimeSpan.Zero;
        }

        membership.EndDate = membership.EndDate.Add(freezeDuration);
        membership.FreezeEndDate = now;
        membership.Status = MembershipStatus.Active;
        membership.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Membership {MembershipId} resumed by user {UserId}. FreezeDurationDays={FreezeDays}.",
            membershipId,
            _currentUserService.UserId ?? "system",
            freezeDuration.TotalDays);

        return membership.Adapt<MembershipReadDto>();
    }

    public async Task<IReadOnlyList<MembershipReadDto>> GetMembershipsForMemberAsync(string memberId)
    {
        await EnsureCanAccessMemberAsync(memberId);

        var membershipRepo = _unitOfWork.Repository<Membership>();
        var memberships = await membershipRepo.ToListAsync(
            membershipRepo.Query()
                .AsNoTracking()
                .Include(m => m.MembershipPlan)
                .Include(m => m.Payments)
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.StartDate));

        return memberships.Adapt<List<MembershipReadDto>>();
    }

    public async Task<WalletBalanceDto> GetWalletBalanceAsync(string memberId)
    {
        await EnsureCanAccessMemberAsync(memberId);

        var memberRepo = _unitOfWork.Repository<Member>();
        var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == memberId));

        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        var balance = await CalculateWalletBalanceAsync(memberId);

        return new WalletBalanceDto
        {
            MemberId = member.Id,
            WalletBalance = balance
        };
    }

    public async Task<IReadOnlyList<WalletTransactionReadDto>> GetWalletTransactionsAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            // Admin can view full history.
        }
        else if (_currentUserService.IsInRole("Member"))
        {
            await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);
        }
        else
        {
            throw new UnauthorizedException("You do not have permission to view wallet transactions.");
        }

        var repo = _unitOfWork.Repository<WalletTransaction>();
        var list = await repo.ToListAsync(
            repo.Query()
                .AsNoTracking()
                .Where(t => t.MemberId == memberId)
                .OrderByDescending(t => t.CreatedAt));

        return list.Adapt<List<WalletTransactionReadDto>>();
    }

    public async Task<WalletBalanceDto> AdjustWalletAsync(AdjustWalletDto dto)
    {
        await _authorizationService.EnsureAdminFullAccessAsync();

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var memberRepo = _unitOfWork.Repository<Member>();
            var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == dto.MemberId));

            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            var currentBalance = await CalculateWalletBalanceAsync(member.Id);
            var updatedBalance = currentBalance + dto.Amount;
            if (updatedBalance < 0)
            {
                throw new AppValidationException("Wallet balance cannot be negative.");
            }

            await AddWalletTransactionAsync(
                member.Id,
                dto.Amount,
                WalletTransactionType.ManualAdjustment,
                null,
                $"Manual wallet adjustment by admin. Amount: {dto.Amount}");

            await _unitOfWork.SaveChangesAsync();
            member.WalletBalance = await CalculateWalletBalanceAsync(member.Id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Wallet adjusted. MemberId={MemberId} Amount={Amount} AdjustedBy={UserId} NewBalance={NewBalance}",
                member.Id,
                dto.Amount,
                _currentUserService.UserId ?? "system",
                member.WalletBalance);

            return new WalletBalanceDto
            {
                MemberId = member.Id,
                WalletBalance = member.WalletBalance
            };
        });
    }

    public async Task<WalletBalanceDto> UseWalletForSessionBookingAsync(UseWalletForSessionBookingDto dto)
    {
        await EnsureCanAccessMemberAsync(dto.MemberId);

        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var session = await sessionRepo.FirstOrDefaultAsync(sessionRepo.Query().Where(s => s.Id == dto.WorkoutSessionId));
        if (session == null)
        {
            throw new NotFoundException("Workout session not found.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var memberRepo = _unitOfWork.Repository<Member>();
            var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == dto.MemberId));
            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            if (member.BranchId.HasValue && session.BranchId.HasValue && member.BranchId.Value != session.BranchId.Value)
            {
                throw new UnauthorizedException("You can only use wallet for sessions in your branch.");
            }

            var currentBalance = await CalculateWalletBalanceAsync(member.Id);
            if (currentBalance < dto.Amount)
            {
                throw new AppValidationException("Insufficient wallet balance.");
            }

            var txRepo = _unitOfWork.Repository<WalletTransaction>();
            var transaction = new WalletTransaction
            {
                MemberId = member.Id,
                Amount = -dto.Amount,
                Type = WalletTransactionType.SessionBooking,
                ReferenceId = dto.WorkoutSessionId,
                Description = $"Wallet deduction for session booking #{dto.WorkoutSessionId}.",
                CreatedByUserId = _currentUserService.UserId
            };
            await txRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var ledgerBalance = await CalculateWalletBalanceAsync(member.Id);
            if (ledgerBalance < 0)
            {
                await txRepo.AddAsync(new WalletTransaction
                {
                    MemberId = member.Id,
                    Amount = dto.Amount,
                    Type = WalletTransactionType.Refund,
                    ReferenceId = dto.WorkoutSessionId,
                    Description = $"Compensation credit for failed concurrent session booking #{dto.WorkoutSessionId}.",
                    CreatedByUserId = _currentUserService.UserId
                });
                await _unitOfWork.SaveChangesAsync();
                throw new AppValidationException("Concurrent wallet debit detected. Please retry.");
            }

            member.WalletBalance = ledgerBalance;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Wallet debited for session booking. MemberId={MemberId} SessionId={SessionId} Amount={Amount} NewBalance={NewBalance}",
                member.Id,
                dto.WorkoutSessionId,
                dto.Amount,
                member.WalletBalance);

            return new WalletBalanceDto
            {
                MemberId = member.Id,
                WalletBalance = member.WalletBalance
            };
        });
    }

    private async Task EnsureCanRequestSubscriptionAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            return;
        }

        await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);

        var memberRepo = _unitOfWork.Repository<Member>();
        var currentMember = await memberRepo.FirstOrDefaultAsync(
            memberRepo.Query().AsNoTracking().Where(m => m.Id == _currentUserService.UserId));
        var targetMember = await memberRepo.FirstOrDefaultAsync(
            memberRepo.Query().AsNoTracking().Where(m => m.Id == memberId));
        if (currentMember != null
            && targetMember != null
            && currentMember.BranchId.HasValue
            && targetMember.BranchId.HasValue
            && currentMember.BranchId.Value != targetMember.BranchId.Value)
        {
            throw new UnauthorizedException("You can only create memberships in your branch.");
        }
    }

    private async Task<(Member Member, MembershipPlan Plan, decimal WalletToUse)> ValidateAndPrepareMembershipContextAsync(
        string memberId,
        int membershipPlanId,
        int? branchId,
        decimal walletAmountToUse)
    {
        var memberRepo = _unitOfWork.Repository<Member>();
        var planRepo = _unitOfWork.Repository<MembershipPlan>();

        var member = await memberRepo.FirstOrDefaultAsync(
            memberRepo.Query().Where(m => m.Id == memberId && m.IsActive));
        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        if (branchId.HasValue && member.BranchId.HasValue && branchId.Value != member.BranchId.Value)
        {
            throw new AppValidationException("Member branch does not match requested branch.");
        }

        var plan = await planRepo.FirstOrDefaultAsync(
            planRepo.Query().Where(p => p.Id == membershipPlanId && p.IsActive));
        if (plan == null)
        {
            throw new NotFoundException("Membership plan not found or inactive.");
        }

        var walletBalance = await CalculateWalletBalanceAsync(member.Id);
        var walletToUse = Math.Min(walletAmountToUse, walletBalance);
        walletToUse = Math.Min(walletToUse, plan.Price);

        return (member, plan, walletToUse);
    }

    private async Task ActivatePendingMembershipInternalAsync(Membership membership, Payment pendingPayment)
    {
        await EnsureNoCommerciallyOpenMembershipAsync(membership.MemberId, membership.Id);

        await ApplyConfirmedPaymentAsync(membership, membership.MembershipPlan, pendingPayment);

        pendingPayment.PaymentStatus = PaymentStatus.Confirmed;
        pendingPayment.PaidAt = DateTime.UtcNow;
        pendingPayment.ReviewedAt = DateTime.UtcNow;
        pendingPayment.RejectionReason = null;
        pendingPayment.ConfirmedByAdminId = _currentUserService.UserId;

        _logger.LogInformation(
            "Membership activated from pending payment. MembershipId={MembershipId} PaymentId={PaymentId} ConfirmedBy={UserId}",
            membership.Id,
            pendingPayment.Id,
            _currentUserService.UserId ?? "system");
    }

    private async Task EnsureCanAccessMemberAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            return;
        }

        var memberRepo = _unitOfWork.Repository<Member>();
        var member = await memberRepo.FirstOrDefaultAsync(
            memberRepo.Query().AsNoTracking().Where(m => m.Id == memberId));
        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        if (_currentUserService.IsInRole("Member"))
        {
            await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);

            var currentMember = await memberRepo.FirstOrDefaultAsync(
                memberRepo.Query().AsNoTracking().Where(m => m.Id == _currentUserService.UserId));
            if (currentMember != null
                && currentMember.BranchId.HasValue
                && member.BranchId.HasValue
                && currentMember.BranchId.Value != member.BranchId.Value)
            {
                throw new UnauthorizedException("You cannot access members from another branch.");
            }
            return;
        }

        if (_currentUserService.IsInRole("Trainer"))
        {
            var currentTrainerId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentTrainerId))
            {
                throw new UnauthorizedException("Authentication required.");
            }

            var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
            var isAssigned = await assignmentRepo.AnyAsync(a => a.TrainerId == currentTrainerId && a.MemberId == memberId);
            if (!isAssigned)
            {
                throw new UnauthorizedException("You can only view wallets for your assigned members.");
            }

            var trainerRepo = _unitOfWork.Repository<Trainer>();
            var trainer = await trainerRepo.FirstOrDefaultAsync(
                trainerRepo.Query().AsNoTracking().Where(t => t.Id == currentTrainerId));
            if (trainer != null
                && trainer.BranchId.HasValue
                && member.BranchId.HasValue
                && trainer.BranchId.Value != member.BranchId.Value)
            {
                throw new UnauthorizedException("You cannot access members from another branch.");
            }

            return;
        }

        throw new UnauthorizedException("You do not have permission to access this resource.");
    }

    private async Task ApplyConfirmedPaymentAsync(Membership membership, MembershipPlan plan, Payment payment)
    {
        var paymentAmount = payment.Amount;
        var coveredByWallet = membership.RemainingBalanceUsedFromWallet;
        var totalCovered = paymentAmount + coveredByWallet;
        var effectivePrice = CalculateEffectivePrice(plan);

        if (totalCovered < effectivePrice)
        {
            throw new AppValidationException("Payment is insufficient for this membership plan.");
        }

        membership.Status = MembershipStatus.Active;
        membership.TotalPaid = paymentAmount;

        var overpaid = totalCovered - effectivePrice;
        if (overpaid > 0)
        {
            await AddWalletTransactionAsync(
                membership.MemberId,
                overpaid,
                WalletTransactionType.Overpayment,
                membership.Id,
                $"Overpayment credited from membership #{membership.Id}.");

            _logger.LogInformation(
                "Membership renewal overpayment credited. MembershipId={MembershipId} MemberId={MemberId} Amount={Amount}",
                membership.Id,
                membership.MemberId,
                overpaid);
        }

        await CreateCommissionAsync(membership, effectivePrice, CommissionSource.Activation);
        await CreateInvoiceAsync(membership, paymentAmount, "PaymentConfirmed", payment.Id);
    }

    private static decimal CalculateEffectivePrice(MembershipPlan plan)
    {
        var raw = plan.Price - (plan.Price * plan.SessionDiscountPercentage / 100m);
        return decimal.Round(Math.Max(raw, 0m), 2, MidpointRounding.AwayFromZero);
    }

    private static PaymentMethod NormalizePaymentMethod(MembershipCreationSource source, PaymentMethod method, decimal walletAmountToUse)
    {
        if (walletAmountToUse > 0)
        {
            return PaymentMethod.Wallet;
        }

        if (source == MembershipCreationSource.Admin &&
            method is PaymentMethod.VodafoneCash or PaymentMethod.Proof or PaymentMethod.PaymentProof)
        {
            return PaymentMethod.Cash;
        }

        if (method == PaymentMethod.VodafoneCash)
        {
            return source == MembershipCreationSource.Admin ? PaymentMethod.Cash : PaymentMethod.Proof;
        }

        if (method == PaymentMethod.PaymentProof)
        {
            return PaymentMethod.Proof;
        }

        return method;
    }

    private async Task AddWalletTransactionAsync(string memberId, decimal amount, WalletTransactionType type, int? referenceId, string description)
    {
        var repo = _unitOfWork.Repository<WalletTransaction>();
        await repo.AddAsync(new WalletTransaction
        {
            MemberId = memberId,
            Amount = amount,
            Type = type,
            ReferenceId = referenceId,
            Description = description,
            CreatedByUserId = _currentUserService.UserId
        });
    }

    private async Task<decimal> CalculateWalletBalanceAsync(string memberId)
    {
        var repo = _unitOfWork.Repository<WalletTransaction>();
        var sum = await repo.Query()
            .Where(t => t.MemberId == memberId)
            .Select(t => (decimal?)t.Amount)
            .SumAsync();

        return sum ?? 0m;
    }

    private async Task SyncWalletProjectionAsync(Member member)
    {
        member.WalletBalance = await CalculateWalletBalanceAsync(member.Id);
    }

    private async Task<MembershipReadDto> GetMembershipWithPaymentsAsync(int membershipId)
    {
        var membershipRepo = _unitOfWork.Repository<Membership>();
        var membership = await membershipRepo.FirstOrDefaultAsync(
            membershipRepo.Query()
                .AsNoTracking()
                .Include(m => m.MembershipPlan)
                .Include(m => m.Payments)
                .Where(m => m.Id == membershipId));

        if (membership == null)
        {
            throw new NotFoundException("Membership not found.");
        }

        return membership.Adapt<MembershipReadDto>();
    }

    private IPaymentGateway ResolveGateway()
    {
        var preferred = Environment.GetEnvironmentVariable("PAYMENT_GATEWAY") ?? "manual";
        return _paymentGateways.FirstOrDefault(g => string.Equals(g.Name, preferred, StringComparison.OrdinalIgnoreCase))
               ?? _paymentGateways.FirstOrDefault(g => g.Name == "manual")
               ?? _paymentGateways.First();
    }

    private async Task CreateCommissionAsync(Membership membership, decimal baseAmount, CommissionSource source)
    {
        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var assignment = await assignmentRepo.FirstOrDefaultAsync(
            assignmentRepo.Query()
                .AsNoTracking()
                .Where(x => x.MemberId == membership.MemberId)
                .OrderByDescending(x => x.AssignedAt));
        if (assignment == null)
        {
            return;
        }

        var amount = decimal.Round(baseAmount * DefaultTrainerCommissionPercent / 100m, 2);
        var commissionRepo = _unitOfWork.Repository<Commission>();
        var alreadyExists = await commissionRepo.AnyAsync(x => x.MembershipId == membership.Id && x.Source == source);
        if (alreadyExists)
        {
            return;
        }

        await commissionRepo.AddAsync(new Commission
        {
            TrainerId = assignment.TrainerId,
            MembershipId = membership.Id,
            BranchId = membership.BranchId,
            Source = source,
            Percentage = DefaultTrainerCommissionPercent,
            CalculatedAmount = amount,
            IsPaid = false
        });

        await AddNotificationAsync(
            assignment.TrainerId,
            "Commission Generated",
            $"Commission {amount:0.00} generated for membership #{membership.Id}.");
    }

    private async Task EnsureNoCommerciallyOpenMembershipAsync(string memberId, int? excludingMembershipId = null)
    {
        var membershipRepo = _unitOfWork.Repository<Membership>();
        var query = membershipRepo.Query()
            .Where(m => m.MemberId == memberId &&
                        (m.Status == MembershipStatus.PendingPayment ||
                         m.Status == MembershipStatus.Active ||
                         m.Status == MembershipStatus.Frozen));

        if (excludingMembershipId.HasValue)
        {
            query = query.Where(m => m.Id != excludingMembershipId.Value);
        }

        var hasOpenMembership = await query.AnyAsync();
        if (hasOpenMembership)
        {
            throw new AppValidationException("Member already has an open membership. Upgrade or wait until current membership expires.");
        }
    }

    private async Task CreateInvoiceAsync(Membership membership, decimal amount, string type, int? paymentId)
    {
        var invoiceRepo = _unitOfWork.Repository<Invoice>();
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32),
            MembershipId = membership.Id,
            PaymentId = paymentId,
            MemberId = membership.MemberId,
            Amount = amount,
            Type = type,
            FilePath = string.Empty
        };

        await invoiceRepo.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        var dto = invoice.Adapt<InvoiceReadDto>();
        var path = await _invoicePdfGenerator.GenerateInvoicePdfAsync(dto);
        invoice.FilePath = path;
    }

    private async Task AddNotificationAsync(string userId, string title, string message)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();
        await notificationRepo.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false
        });
    }
}
