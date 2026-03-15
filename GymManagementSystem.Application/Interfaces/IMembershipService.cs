using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IMembershipService
{
    Task<MembershipPlanReadDto> CreateMembershipPlanAsync(CreateMembershipPlanDto dto);
    Task<MembershipPlanReadDto> UpdateMembershipPlanAsync(UpdateMembershipPlanDto dto);
    Task<IReadOnlyList<MembershipPlanReadDto>> GetMembershipPlansAsync(bool activeOnly);
    Task<MembershipReadDto> RequestSubscriptionAsync(RequestSubscriptionDto dto);
    Task<MembershipReadDto> ActivatePendingMembershipAsync(int membershipId);
    Task<MembershipReadDto> CreateDirectMembershipAsync(CreateDirectMembershipDto dto);
    Task<MembershipReadDto> CreateMembershipAsync(CreateMembershipCommand command);
    Task<MembershipReadDto> CreateMembershipAsync(CreateMembershipDto dto);
    Task<MembershipReadDto> UpgradeMembershipAsync(UpgradeMembershipDto dto);
    Task<MembershipReadDto> ConfirmPaymentAsync(int paymentId, ConfirmPaymentDto dto);
    Task<MembershipReadDto> RejectPaymentAsync(int paymentId);
    Task<PaymentReadDto> UploadPaymentProofAsync(UploadPaymentProofDto dto);
    Task<IReadOnlyList<PendingPaymentReadDto>> GetPendingPaymentsAsync();
    Task<MembershipReadDto> ReviewPaymentAsync(int paymentId, ReviewPaymentDto dto);
    Task<MembershipReadDto> FreezeMembershipAsync(int membershipId, FreezeMembershipDto dto);
    Task<MembershipReadDto> ResumeMembershipAsync(int membershipId);
    Task<IReadOnlyList<MembershipReadDto>> GetMembershipsForMemberAsync(string memberId);
    Task<WalletBalanceDto> GetWalletBalanceAsync(string memberId);
    Task<IReadOnlyList<WalletTransactionReadDto>> GetWalletTransactionsAsync(string memberId);
    Task<WalletBalanceDto> AdjustWalletAsync(AdjustWalletDto dto);
    Task<WalletBalanceDto> UseWalletForSessionBookingAsync(UseWalletForSessionBookingDto dto);
}

public interface ISubscriptionAutomationService
{
    Task<SubscriptionAutomationResultDto> ProcessExpirationsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public interface IRevenueMetricsService
{
    Task<int> GetActiveMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<int> GetExpiringSoonMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<int> GetExpiredMembershipCountAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyRecurringRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSessionRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalMembershipRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAddOnRevenueAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetWalletTotalBalanceAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevenueByBranchTypeDto>> GetRevenuePerBranchByTypeAsync(CancellationToken cancellationToken = default);
    Task<RevenueMetricsDto> GetDashboardMetricsAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<FinancialOverviewDto> GetFinancialOverviewAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopSellingMembershipPlanDto>> GetTopPlansAsync(int top = 5, int? branchId = null, CancellationToken cancellationToken = default);
}
