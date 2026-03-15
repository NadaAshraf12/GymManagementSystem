using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public SessionService(IUnitOfWork unitOfWork, IAppAuthorizationService authorizationService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionDto dto)
    {
        await _authorizationService.EnsureTrainerOwnsResourceAsync(dto.TrainerId);

        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainer = await trainerRepo.FirstOrDefaultAsync(trainerRepo.Query().Where(t => t.Id == dto.TrainerId));
        if (trainer == null)
        {
            throw new KeyNotFoundException("Trainer not found.");
        }

        if (dto.BranchId.HasValue && trainer.BranchId.HasValue && dto.BranchId.Value != trainer.BranchId.Value)
        {
            throw new KeyNotFoundException("Trainer cannot create session in another branch.");
        }

        var session = dto.Adapt<WorkoutSession>();
        session.BranchId = dto.BranchId ?? trainer.BranchId;
        session.CurrentParticipants = 0;

        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        await sessionRepo.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return session.Adapt<WorkoutSessionDto>();
    }

    public async Task<WorkoutSessionDto> UpdateAsync(UpdateWorkoutSessionDto dto)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == dto.Id));

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        await _authorizationService.EnsureTrainerOwnsResourceAsync(session.TrainerId);

        dto.Adapt(session);
        await _unitOfWork.SaveChangesAsync();

        return session.Adapt<WorkoutSessionDto>();
    }

    public async Task<bool> BookMemberAsync(BookMemberToSessionDto dto)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();

        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == dto.WorkoutSessionId));
        if (session == null)
        {
            return false;
        }

        if (session.Price > 0)
        {
            return false;
        }

        if (session.CurrentParticipants >= session.MaxParticipants)
        {
            return false;
        }

        var memberRepo = _unitOfWork.Repository<Member>();
        var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == dto.MemberId));
        if (member == null)
        {
            return false;
        }

        if (member.BranchId.HasValue && session.BranchId.HasValue && member.BranchId.Value != session.BranchId.Value)
        {
            return false;
        }

        var benefitContext = await GetActiveMembershipBenefitContextAsync(dto.MemberId);
        if (!CanUsePriorityBooking(session, benefitContext))
        {
            return false;
        }

        var alreadyBooked = await memberSessionRepo.AnyAsync(ms =>
            ms.WorkoutSessionId == dto.WorkoutSessionId && ms.MemberId == dto.MemberId);
        if (alreadyBooked)
        {
            return true;
        }

        var booking = new MemberSession
        {
            MemberId = dto.MemberId,
            WorkoutSessionId = dto.WorkoutSessionId,
            BookingDate = DateTime.UtcNow,
            Attended = false,
            OriginalPrice = 0,
            ChargedPrice = 0,
            AppliedDiscountPercentage = 0,
            UsedIncludedSession = false,
            PriorityBookingApplied = benefitContext?.Plan.PriorityBooking ?? false
        };
        await memberSessionRepo.AddAsync(booking);
        session.CurrentParticipants += 1;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<SessionBookingResultDto> BookPaidSessionAsync(PaidSessionBookingDto dto)
    {
        await EnsureMemberOwnershipAsync(dto.MemberId);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
            var memberRepo = _unitOfWork.Repository<Member>();
            var memberSessionRepo = _unitOfWork.Repository<MemberSession>();
            var walletRepo = _unitOfWork.Repository<WalletTransaction>();

            var session = await sessionRepo.FirstOrDefaultAsync(
                sessionRepo.Query().Where(s => s.Id == dto.WorkoutSessionId));
            if (session == null)
            {
                throw new NotFoundException("Workout session not found.");
            }

            if (session.Price <= 0)
            {
                throw new AppValidationException("Session is free. Use regular booking.");
            }

            if (session.CurrentParticipants >= session.MaxParticipants)
            {
                throw new AppValidationException("Session is full.");
            }

            var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().Where(m => m.Id == dto.MemberId));
            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            if (member.BranchId.HasValue && session.BranchId.HasValue && member.BranchId.Value != session.BranchId.Value)
            {
                throw new UnauthorizedException("Cannot book session in another branch.");
            }

            var alreadyBooked = await memberSessionRepo.FirstOrDefaultAsync(memberSessionRepo.Query()
                .AsNoTracking()
                .Where(ms => ms.WorkoutSessionId == dto.WorkoutSessionId && ms.MemberId == dto.MemberId));
            if (alreadyBooked != null)
            {
                var existingBalance = await CalculateWalletBalanceAsync(dto.MemberId);
                return new SessionBookingResultDto
                {
                    Success = true,
                    Message = "Already booked.",
                    WalletBalance = existingBalance,
                    OriginalPrice = alreadyBooked.OriginalPrice,
                    ChargedPrice = alreadyBooked.ChargedPrice,
                    UsedIncludedSession = alreadyBooked.UsedIncludedSession,
                    DiscountPercentageApplied = alreadyBooked.AppliedDiscountPercentage,
                    PriorityBookingApplied = alreadyBooked.PriorityBookingApplied
                };
            }

            var benefitContext = await GetActiveMembershipBenefitContextAsync(dto.MemberId);
            if (!CanUsePriorityBooking(session, benefitContext))
            {
                throw new AppValidationException("Only members with priority booking can reserve the last available slot.");
            }

            var usedIncludedThisMonth = await GetUsedIncludedSessionsThisMonthAsync(dto.MemberId, DateTime.UtcNow);
            var pricing = BuildPricingPreview(session, benefitContext, usedIncludedThisMonth);

            var balance = await CalculateWalletBalanceAsync(dto.MemberId);
            if (pricing.FinalPrice > 0 && balance < pricing.FinalPrice)
            {
                return new SessionBookingResultDto
                {
                    Success = false,
                    Message = "Insufficient wallet balance.",
                    WalletBalance = balance,
                    OriginalPrice = pricing.OriginalPrice,
                    ChargedPrice = pricing.FinalPrice,
                    UsedIncludedSession = pricing.IsIncludedSessionApplied,
                    DiscountPercentageApplied = pricing.DiscountPercentage,
                    PriorityBookingApplied = pricing.PriorityBookingEnabled
                };
            }

            if (pricing.FinalPrice > 0)
            {
                await walletRepo.AddAsync(new WalletTransaction
                {
                    MemberId = dto.MemberId,
                    Amount = -pricing.FinalPrice,
                    Type = WalletTransactionType.SessionBooking,
                    ReferenceId = session.Id,
                    Description = $"Wallet debit for paid session #{session.Id}.",
                    CreatedByUserId = _currentUserService.UserId
                });
            }

            await memberSessionRepo.AddAsync(new MemberSession
            {
                MemberId = dto.MemberId,
                WorkoutSessionId = dto.WorkoutSessionId,
                BookingDate = DateTime.UtcNow,
                Attended = false,
                OriginalPrice = pricing.OriginalPrice,
                ChargedPrice = pricing.FinalPrice,
                AppliedDiscountPercentage = pricing.DiscountPercentage,
                UsedIncludedSession = pricing.IsIncludedSessionApplied,
                PriorityBookingApplied = pricing.PriorityBookingEnabled
            });
            session.CurrentParticipants += 1;

            await _unitOfWork.SaveChangesAsync();

            var newBalance = await CalculateWalletBalanceAsync(dto.MemberId);
            if (newBalance < 0 && pricing.FinalPrice > 0)
            {
                await walletRepo.AddAsync(new WalletTransaction
                {
                    MemberId = dto.MemberId,
                    Amount = pricing.FinalPrice,
                    Type = WalletTransactionType.Refund,
                    ReferenceId = session.Id,
                    Description = $"Compensation credit for failed concurrent paid session booking #{session.Id}.",
                    CreatedByUserId = _currentUserService.UserId
                });
                await _unitOfWork.SaveChangesAsync();
                throw new AppValidationException("Concurrent wallet conflict detected.");
            }

            member.WalletBalance = newBalance;
            await _unitOfWork.SaveChangesAsync();

            return new SessionBookingResultDto
            {
                Success = true,
                Message = pricing.IsIncludedSessionApplied
                    ? "Session booked successfully using included monthly session."
                    : "Paid session booked successfully.",
                WalletBalance = newBalance,
                OriginalPrice = pricing.OriginalPrice,
                ChargedPrice = pricing.FinalPrice,
                UsedIncludedSession = pricing.IsIncludedSessionApplied,
                DiscountPercentageApplied = pricing.DiscountPercentage,
                PriorityBookingApplied = pricing.PriorityBookingEnabled
            };
        });
    }

    public async Task<bool> CancelBookingAsync(string memberId, int workoutSessionId)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var memberSessionRepo = _unitOfWork.Repository<MemberSession>();

        var booking = await memberSessionRepo.FirstOrDefaultAsync(
            memberSessionRepo.Query().Where(ms => ms.WorkoutSessionId == workoutSessionId && ms.MemberId == memberId));
        if (booking == null)
        {
            return false;
        }

        memberSessionRepo.Remove(booking);
        var session = await sessionRepo.FirstOrDefaultAsync(
            sessionRepo.Query().Where(s => s.Id == workoutSessionId));
        if (session != null && session.CurrentParticipants > 0)
        {
            session.CurrentParticipants -= 1;
        }
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByTrainerAsync(string trainerId, DateTime? from = null, DateTime? to = null)
    {
        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var query = sessionRepo.Query().Where(s => s.TrainerId == trainerId);
        if (from.HasValue) query = query.Where(s => s.SessionDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SessionDate <= to.Value);
        var list = await sessionRepo.ToListAsync(query.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime));
        return list.Adapt<List<WorkoutSessionDto>>();
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetAvailableForMemberAsync(string memberId, DateTime? from = null, DateTime? to = null)
    {
        await EnsureMemberOwnershipAsync(memberId);

        var memberRepo = _unitOfWork.Repository<Member>();
        var member = await memberRepo.FirstOrDefaultAsync(memberRepo.Query().AsNoTracking().Where(m => m.Id == memberId));
        if (member == null)
        {
            throw new NotFoundException("Member not found.");
        }

        var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
        var query = sessionRepo.Query()
            .Where(s => s.SessionDate >= DateTime.UtcNow.Date)
            .AsQueryable();
        if (from.HasValue) query = query.Where(s => s.SessionDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SessionDate <= to.Value);
        if (member.BranchId.HasValue) query = query.Where(s => s.BranchId == member.BranchId);

        var list = await sessionRepo.ToListAsync(query.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime));
        return list.Adapt<List<WorkoutSessionDto>>();
    }

    public async Task<IReadOnlyList<SessionPricingPreviewDto>> GetSessionPricingPreviewsAsync(string memberId, DateTime? from = null, DateTime? to = null)
    {
        await EnsureMemberOwnershipAsync(memberId);

        var sessions = await GetAvailableForMemberAsync(memberId, from, to);
        var benefitContext = await GetActiveMembershipBenefitContextAsync(memberId);
        var usedIncluded = await GetUsedIncludedSessionsThisMonthAsync(memberId, DateTime.UtcNow);
        var remainingIncluded = Math.Max(0, (benefitContext?.Plan.IncludedSessionsPerMonth ?? 0) - usedIncluded);

        return sessions
            .Select(s =>
            {
                var preview = BuildPricingPreview(
                    new WorkoutSession
                    {
                        Id = s.Id,
                        Price = s.Price,
                        SessionDate = s.SessionDate,
                        StartTime = s.StartTime
                    },
                    benefitContext,
                    usedIncluded);
                preview.RemainingIncludedSessionsThisMonth = remainingIncluded;
                return preview;
            })
            .ToList();
    }

    public async Task<MembershipBenefitsSnapshotDto> GetMembershipBenefitsSnapshotAsync(string memberId)
    {
        await EnsureMemberOwnershipAsync(memberId);

        var context = await GetActiveMembershipBenefitContextAsync(memberId);
        var usedIncluded = await GetUsedIncludedSessionsThisMonthAsync(memberId, DateTime.UtcNow);
        var included = context?.Plan.IncludedSessionsPerMonth ?? 0;

        return new MembershipBenefitsSnapshotDto
        {
            HasActiveMembership = context != null,
            ActivePlanName = context?.Plan.Name ?? string.Empty,
            IncludedSessionsPerMonth = included,
            UsedIncludedSessionsThisMonth = usedIncluded,
            RemainingIncludedSessionsThisMonth = Math.Max(0, included - usedIncluded),
            SessionDiscountPercentage = context?.Plan.SessionDiscountPercentage ?? 0,
            PriorityBooking = context?.Plan.PriorityBooking ?? false,
            AddOnAccess = context?.Plan.AddOnAccess ?? false
        };
    }

    private async Task EnsureMemberOwnershipAsync(string memberId)
    {
        if (_currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (_currentUserService.IsInRole("Member"))
        {
            await _authorizationService.EnsureMemberOwnsResourceAsync(memberId);
            return;
        }

        throw new UnauthorizedException("Only member/admin can book this session.");
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

    private async Task<MembershipBenefitContext?> GetActiveMembershipBenefitContextAsync(string memberId)
    {
        var membershipRepo = _unitOfWork.Repository<Membership>();
        var membership = await membershipRepo.FirstOrDefaultAsync(
            membershipRepo.Query()
                .AsNoTracking()
                .Include(m => m.MembershipPlan)
                .Where(m =>
                    m.MemberId == memberId &&
                    m.Status == MembershipStatus.Active &&
                    m.StartDate <= DateTime.UtcNow &&
                    m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate));

        if (membership == null)
        {
            return null;
        }

        return new MembershipBenefitContext(membership, membership.MembershipPlan);
    }

    private async Task<int> GetUsedIncludedSessionsThisMonthAsync(string memberId, DateTime utcNow)
    {
        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var repo = _unitOfWork.Repository<MemberSession>();
        return await repo.Query()
            .AsNoTracking()
            .Where(ms =>
                ms.MemberId == memberId &&
                ms.UsedIncludedSession &&
                ms.BookingDate >= monthStart &&
                ms.BookingDate < nextMonth)
            .CountAsync();
    }

    private static bool CanUsePriorityBooking(WorkoutSession session, MembershipBenefitContext? context)
    {
        var remaining = session.MaxParticipants - session.CurrentParticipants;
        if (remaining > 1)
        {
            return true;
        }

        return context?.Plan.PriorityBooking ?? false;
    }

    private static SessionPricingPreviewDto BuildPricingPreview(
        WorkoutSession session,
        MembershipBenefitContext? context,
        int usedIncludedSessionsThisMonth)
    {
        var original = session.Price;
        if (original <= 0)
        {
            return new SessionPricingPreviewDto
            {
                WorkoutSessionId = session.Id,
                OriginalPrice = 0,
                FinalPrice = 0,
                RemainingIncludedSessionsThisMonth = 0,
                DiscountPercentage = 0,
                PriorityBookingEnabled = context?.Plan.PriorityBooking ?? false,
                IsIncludedSessionApplied = false
            };
        }

        var included = context?.Plan.IncludedSessionsPerMonth ?? 0;
        var remainingIncluded = Math.Max(0, included - usedIncludedSessionsThisMonth);
        if (remainingIncluded > 0)
        {
            return new SessionPricingPreviewDto
            {
                WorkoutSessionId = session.Id,
                OriginalPrice = original,
                FinalPrice = 0,
                RemainingIncludedSessionsThisMonth = remainingIncluded - 1,
                DiscountPercentage = 0,
                PriorityBookingEnabled = context?.Plan.PriorityBooking ?? false,
                IsIncludedSessionApplied = true
            };
        }

        var discount = context?.Plan.SessionDiscountPercentage ?? 0;
        if (discount <= 0)
        {
            return new SessionPricingPreviewDto
            {
                WorkoutSessionId = session.Id,
                OriginalPrice = original,
                FinalPrice = original,
                RemainingIncludedSessionsThisMonth = remainingIncluded,
                DiscountPercentage = 0,
                PriorityBookingEnabled = context?.Plan.PriorityBooking ?? false,
                IsIncludedSessionApplied = false
            };
        }

        var discountAmount = decimal.Round(original * discount / 100m, 2, MidpointRounding.AwayFromZero);
        var final = Math.Max(0, original - discountAmount);
        return new SessionPricingPreviewDto
        {
            WorkoutSessionId = session.Id,
            OriginalPrice = original,
            FinalPrice = final,
            RemainingIncludedSessionsThisMonth = remainingIncluded,
            DiscountPercentage = discount,
            PriorityBookingEnabled = context?.Plan.PriorityBooking ?? false,
            IsIncludedSessionApplied = false
        };
    }

    private sealed record MembershipBenefitContext(Membership Membership, MembershipPlan Plan);
}
