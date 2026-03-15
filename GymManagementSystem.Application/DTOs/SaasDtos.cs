namespace GymManagementSystem.Application.DTOs;

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class BranchReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AssignUserBranchDto
{
    public string UserId { get; set; } = string.Empty;
    public int BranchId { get; set; }
}

public class CommissionReadDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int MembershipId { get; set; }
    public int? BranchId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal CalculatedAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidByAdminId { get; set; }
}

public class TrainerCommissionMetricsDto
{
    public string TrainerId { get; set; } = string.Empty;
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
}

public class TrainerCommissionDashboardDto
{
    public string TrainerId { get; set; } = string.Empty;
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
    public List<CommissionReadDto> RecentCommissions { get; set; } = new();
}

public class InvoiceReadDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? MembershipId { get; set; }
    public int? AddOnId { get; set; }
    public int? PaymentId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class NotificationReadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAddOnDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? BranchId { get; set; }
    public bool RequiresActiveMembership { get; set; }
}

public class AddOnReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? BranchId { get; set; }
    public bool RequiresActiveMembership { get; set; }
}

public class PurchaseAddOnDto
{
    public string MemberId { get; set; } = string.Empty;
    public int AddOnId { get; set; }
}

public class RevenueByBranchTypeDto
{
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal MembershipRevenue { get; set; }
    public decimal SessionRevenue { get; set; }
    public decimal AddOnRevenue { get; set; }
}

public class MarkNotificationReadDto
{
    public int NotificationId { get; set; }
}
