using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.WebUI.Models;

public class AdminDashboardViewModel
{
    public int? SelectedBranchId { get; set; }
    public List<BranchOptionViewModel> Branches { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalSessionRevenue { get; set; }
    public decimal TotalMembershipRevenue { get; set; }
    public decimal TotalAddOnRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal TotalWalletBalance { get; set; }
    public int ActiveMemberships { get; set; }
    public int ExpiringSoonMemberships { get; set; }
    public int ExpiredMemberships { get; set; }
    public decimal TotalCommissionsOwed { get; set; }
    public decimal TotalCommissionsPaid { get; set; }
    public decimal WalletTotalCredits { get; set; }
    public decimal WalletTotalDebits { get; set; }
}

public class AdminRevenueDashboardViewModel
{
    public int? SelectedBranchId { get; set; }
    public List<BranchOptionViewModel> Branches { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal WalletCashIn { get; set; }
    public decimal MembershipRevenue { get; set; }
    public int ActiveMemberships { get; set; }
    public List<TopPlanItemViewModel> TopPlans { get; set; } = new();
    public List<ExpiringMembershipItemViewModel> ExpiringSoon { get; set; } = new();
    public List<RevenuePointItemViewModel> RevenueLast30Days { get; set; } = new();
}

public class TopPlanItemViewModel
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int ActivationCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ExpiringMembershipItemViewModel
{
    public int MembershipId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
}

public class RevenuePointItemViewModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class BranchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MemberPortalViewModel
{
    public List<MembershipListItemViewModel> Memberships { get; set; } = new();
    public MembershipListItemViewModel? CurrentMembership { get; set; }
    public decimal WalletBalance { get; set; }
    public MembershipBenefitsViewModel Benefits { get; set; } = new();
    public List<WalletTransactionItemViewModel> WalletTransactions { get; set; } = new();
    public int WalletTransactionsPage { get; set; }
    public int WalletTransactionsPageSize { get; set; }
    public int WalletTransactionsTotalCount { get; set; }
    public List<InvoiceItemViewModel> Invoices { get; set; } = new();
    public List<NotificationItemViewModel> Notifications { get; set; } = new();
    public List<WorkoutSessionOptionViewModel> AvailablePaidSessions { get; set; } = new();
    public List<MembershipPlanOptionViewModel> UpgradePlans { get; set; } = new();
    public List<PlanCardViewModel> AvailablePlans { get; set; } = new();
    public List<AddOnOptionViewModel> AvailableAddOns { get; set; } = new();
}

public class MembershipListItemViewModel
{
    public int Id { get; set; }
    public int MembershipPlanId { get; set; }
    public string MembershipPlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPaid { get; set; }
    public string StatusBadgeClass { get; set; } = "bg-secondary";
}

public class WalletTransactionItemViewModel
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class InvoiceItemViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class NotificationItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BranchManagementViewModel
{
    public CreateBranchFormViewModel CreateBranch { get; set; } = new();
    public AssignBranchFormViewModel AssignMember { get; set; } = new();
    public AssignBranchFormViewModel AssignTrainer { get; set; } = new();
    public List<BranchReadItemViewModel> Branches { get; set; } = new();
    public List<SimpleUserOptionViewModel> Members { get; set; } = new();
    public List<SimpleUserOptionViewModel> Trainers { get; set; } = new();
}

public class CreateBranchFormViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class AssignBranchFormViewModel
{
    public string UserId { get; set; } = string.Empty;
    public int BranchId { get; set; }
}

public class BranchReadItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SimpleUserOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}

public class CommissionCenterViewModel
{
    public List<CommissionItemViewModel> UnpaidCommissions { get; set; } = new();
    public List<TrainerCommissionMetricsItemViewModel> Metrics { get; set; } = new();
}

public class CommissionItemViewModel
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int MembershipId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MembershipPlanName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceBadgeClass { get; set; } = "bg-secondary";
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public decimal Percentage { get; set; }
    public decimal CalculatedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TrainerCommissionMetricsItemViewModel
{
    public string TrainerId { get; set; } = string.Empty;
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
}

public class MembershipManagementViewModel
{
    public string? StatusFilter { get; set; }
    public int? PlanIdFilter { get; set; }
    public int? BranchIdFilter { get; set; }
    public List<BranchOptionViewModel> Branches { get; set; } = new();
    public List<MembershipPlanOptionViewModel> Plans { get; set; } = new();
    public List<PendingPaymentItemViewModel> PendingPayments { get; set; } = new();
    public List<MembershipStatusItemViewModel> ActiveMemberships { get; set; } = new();
    public List<MembershipStatusItemViewModel> FrozenMemberships { get; set; } = new();
    public List<MembershipStatusItemViewModel> ExpiredMemberships { get; set; } = new();
    public List<MembershipStatusItemViewModel> CancelledMemberships { get; set; } = new();
    public List<PlanCardViewModel> MembershipPlans { get; set; } = new();
    public List<SimpleUserOptionViewModel> Members { get; set; } = new();
    public List<MembershipStatusItemViewModel> MembershipStatuses { get; set; } = new();
    public WalletAdjustFormViewModel WalletAdjust { get; set; } = new();
    public FreezeMembershipFormViewModel FreezeForm { get; set; } = new();
    public ResumeMembershipFormViewModel ResumeForm { get; set; } = new();
}

public class PendingPaymentItemViewModel
{
    public int PaymentId { get; set; }
    public int MembershipId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string MemberDisplayName { get; set; } = string.Empty;
    public string MembershipPlanName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentProofUrl { get; set; }
}

public class WalletAdjustFormViewModel
{
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FreezeMembershipFormViewModel
{
    public int MembershipId { get; set; }
    public DateTime FreezeStartDate { get; set; } = DateTime.UtcNow;
}

public class ResumeMembershipFormViewModel
{
    public int MembershipId { get; set; }
}

public class MembershipStatusItemViewModel
{
    public int MembershipId { get; set; }
    public int MembershipPlanId { get; set; }
    public int? BranchId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string MemberDisplayName { get; set; } = string.Empty;
    public string MembershipPlanName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentStatus { get; set; } = "N/A";
    public string Source { get; set; } = "N/A";
    public decimal WalletBalance { get; set; }
}

public class WorkoutSessionOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IncludedApplied { get; set; }
    public decimal DiscountPercentage { get; set; }
}

public class MembershipPlanOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationInDays { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int IncludedSessionsPerMonth { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; }
    public string CommissionRate { get; set; } = "10%";
}

public class PlanCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int IncludedSessionsPerMonth { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; }
    public string CommissionRate { get; set; } = "10%";
    public bool IsActive { get; set; }
}

public class AddOnOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool RequiresActiveMembership { get; set; }
}

public class MembershipBenefitsViewModel
{
    public bool HasActiveMembership { get; set; }
    public string ActivePlanName { get; set; } = string.Empty;
    public int RemainingFreeSessionsThisMonth { get; set; }
    public decimal SessionDiscountPercentage { get; set; }
    public bool PriorityBooking { get; set; }
    public bool AddOnAccess { get; set; }
}

public class CreateMembershipViewModel
{
    public string MemberId { get; set; } = string.Empty;
    public int MembershipPlanId { get; set; }
    public int? BranchId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public List<SimpleUserOptionViewModel> Members { get; set; } = new();
    public List<BranchOptionViewModel> Branches { get; set; } = new();
    public List<MembershipPlanOptionViewModel> Plans { get; set; } = new();
    public Dictionary<string, decimal> WalletBalancesByMemberId { get; set; } = new();
}

public class WalletTopUpViewModel
{
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public List<SimpleUserOptionViewModel> Members { get; set; } = new();
    public Dictionary<string, decimal> WalletBalancesByMemberId { get; set; } = new();
}

public class MembershipPlansAdminIndexViewModel
{
    public List<MembershipPlanReadDtoItemViewModel> Plans { get; set; } = new();
}

public class MembershipPlanReadDtoItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }
}

public class MembershipPlanFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; } = true;
}
