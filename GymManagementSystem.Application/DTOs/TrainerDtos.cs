using System;

namespace GymManagementSystem.Application.DTOs;

public class CreateTrainerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTrainerDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerReadDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerDto
{
    public string? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerAssignmentDetailDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
}

public class TrainerFinancialProfileDto
{
    public string TrainerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    public decimal TotalGeneratedCommission { get; set; }
    public decimal TotalPaidCommission { get; set; }
    public decimal TotalPendingCommission { get; set; }
    public decimal MembershipRevenueFromTrainerMembers { get; set; }
    public decimal SessionRevenue { get; set; }

    public List<TrainerCommissionFinancialRowDto> Commissions { get; set; } = new();
    public List<TrainerSessionEarningRowDto> SessionEarnings { get; set; } = new();
    public List<TrainerMembershipRevenueRowDto> MembershipRevenues { get; set; } = new();
    public List<TrainerFinancialTransactionDto> RecentTransactions { get; set; } = new();
    public List<TrainerCommissionTrendPointDto> CommissionLast30Days { get; set; } = new();
}

public class TrainerCommissionFinancialRowDto
{
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MembershipPlanName { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TrainerSessionEarningRowDto
{
    public string SessionTitle { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal TrainerShare { get; set; }
    public DateTime Date { get; set; }
}

public class TrainerMembershipRevenueRowDto
{
    public string MemberName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal RevenueAmount { get; set; }
    public DateTime StartDate { get; set; }
}

public class TrainerFinancialTransactionDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TrainerCommissionTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

