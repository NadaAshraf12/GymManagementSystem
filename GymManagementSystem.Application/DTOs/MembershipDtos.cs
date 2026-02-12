using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Application.DTOs;

public class CreateMembershipPlanDto
{
    public string Name { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
    public int DurationDays
    {
        get => DurationInDays;
        set => DurationInDays = value;
    }
    public decimal Price { get; set; }
    public decimal CommissionRate { get; set; }
    public int IncludedSessionsPerMonth { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMembershipPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
    public int DurationDays
    {
        get => DurationInDays;
        set => DurationInDays = value;
    }
    public decimal Price { get; set; }
    public decimal CommissionRate { get; set; }
    public int IncludedSessionsPerMonth { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MembershipPlanReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
    public int DurationDays
    {
        get => DurationInDays;
        set => DurationInDays = value;
    }
    public decimal Price { get; set; }
    public decimal CommissionRate { get; set; }
    public int IncludedSessionsPerMonth { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMembershipDto
{
    public string MemberId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public int MembershipPlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public MembershipSource Source { get; set; } = MembershipSource.Online;
    public bool AutoRenewEnabled { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal WalletAmountToUse { get; set; }
}

public class RequestSubscriptionDto
{
    public string MemberId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public int MembershipPlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public bool AutoRenewEnabled { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal WalletAmountToUse { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VodafoneCash;
}

public class CreateDirectMembershipDto
{
    public string MemberId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public int MembershipPlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public bool AutoRenewEnabled { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal WalletAmountToUse { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VodafoneCash;
}

public class UpgradeMembershipDto
{
    public string MemberId { get; set; } = string.Empty;
    public int NewMembershipPlanId { get; set; }
}

public class MembershipReadDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public int MembershipPlanId { get; set; }
    public string MembershipPlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MembershipStatus Status { get; set; }
    public MembershipSource Source { get; set; }
    public bool AutoRenewEnabled { get; set; }
    public DateTime? FreezeStartDate { get; set; }
    public DateTime? FreezeEndDate { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalanceUsedFromWallet { get; set; }
    public List<PaymentReadDto> Payments { get; set; } = new();
}

public class PaymentReadDto
{
    public int Id { get; set; }
    public int MembershipId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? PaymentProofUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ConfirmedByAdminId { get; set; }
}

public class ConfirmPaymentDto
{
    public decimal? ConfirmedAmount { get; set; }
}

public class UploadPaymentProofDto
{
    public int PaymentId { get; set; }
    public string PaymentProofUrl { get; set; } = string.Empty;
}

public class ReviewPaymentDto
{
    public bool Approve { get; set; }
    public decimal? ConfirmedAmount { get; set; }
    public string? RejectionReason { get; set; }
}

public class PendingPaymentReadDto
{
    public int PaymentId { get; set; }
    public int MembershipId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? PaymentProofUrl { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class FreezeMembershipDto
{
    public DateTime FreezeStartDate { get; set; } = DateTime.UtcNow;
}

public class AdjustWalletDto
{
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class WalletBalanceDto
{
    public string MemberId { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
}

public class WalletTransactionReadDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletTransactionType Type { get; set; }
    public int? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
}

public class SubscriptionAutomationResultDto
{
    public int ExpiredCount { get; set; }
    public int AutoRenewedCount { get; set; }
    public int AutoRenewSkippedCount { get; set; }
}

public class RevenueMetricsDto
{
    public int? BranchId { get; set; }
    public int ActiveMemberships { get; set; }
    public int ExpiringSoonMemberships { get; set; }
    public int ExpiredMemberships { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalSessionRevenue { get; set; }
    public decimal TotalMembershipRevenue { get; set; }
    public decimal TotalAddOnRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal TotalWalletBalance { get; set; }
    public decimal TotalCommissionsOwed { get; set; }
    public decimal TotalCommissionsPaid { get; set; }
    public decimal WalletTotalCredits { get; set; }
    public decimal WalletTotalDebits { get; set; }
}

public class UseWalletForSessionBookingDto
{
    public string MemberId { get; set; } = string.Empty;
    public int WorkoutSessionId { get; set; }
    public decimal Amount { get; set; }
}
