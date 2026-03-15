using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces;

public interface IBranchService
{
    Task<BranchReadDto> CreateAsync(CreateBranchDto dto);
    Task<IReadOnlyList<BranchReadDto>> GetAllAsync();
    Task AssignMemberAsync(AssignUserBranchDto dto);
    Task AssignTrainerAsync(AssignUserBranchDto dto);
}

public interface ICommissionService
{
    Task<IReadOnlyList<CommissionReadDto>> GetUnpaidAsync();
    Task<CommissionReadDto> MarkPaidAsync(int commissionId);
    Task<IReadOnlyList<TrainerCommissionMetricsDto>> GetTrainerMetricsAsync();
    Task<TrainerCommissionDashboardDto> GetMyDashboardAsync();
}

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceReadDto>> GetMemberInvoicesAsync(string memberId);
    Task<InvoiceReadDto> GetByIdAsync(int id);
    Task<(byte[] Content, string FileName, string ContentType)> DownloadAsync(int id);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationReadDto>> GetMyNotificationsAsync();
    Task MarkReadAsync(MarkNotificationReadDto dto);
}

public interface IAddOnService
{
    Task<AddOnReadDto> CreateAsync(CreateAddOnDto dto);
    Task<IReadOnlyList<AddOnReadDto>> GetAvailableForMemberAsync(string memberId);
    Task<InvoiceReadDto> PurchaseAsync(PurchaseAddOnDto dto);
}

public interface IPaymentGateway
{
    string Name { get; }
    Task<(GymManagementSystem.Domain.Enums.PaymentMethod Method, GymManagementSystem.Domain.Enums.PaymentStatus Status)> PrepareAsync(GymManagementSystem.Domain.Enums.MembershipSource source, CancellationToken cancellationToken = default);
}

public interface IInvoicePdfGenerator
{
    Task<string> GenerateInvoicePdfAsync(InvoiceReadDto dto, CancellationToken cancellationToken = default);
}
