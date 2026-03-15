using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.WebUI.Models;

public class TrainerMemberListItem
{
    public string MemberId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public int TrainingCompleted { get; set; }
    public int TrainingTotal { get; set; }
    public int NutritionCompleted { get; set; }
    public int NutritionTotal { get; set; }
}

public class CreateTrainingPlanViewModel
{
    [Required]
    public string MemberId { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateTrainingPlanItemViewModel> Items { get; set; } = new();
}

public class CreateTrainingPlanItemViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public string ExerciseName { get; set; } = string.Empty;
    [Range(1, 20)]
    public int Sets { get; set; }
    [Range(1, 100)]
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public class CreateNutritionPlanViewModel
{
    [Required]
    public string MemberId { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateNutritionPlanItemViewModel> Items { get; set; } = new();
}

public class CreateNutritionPlanItemViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public string MealType { get; set; } = string.Empty;
    [Required]
    [MaxLength(500)]
    public string FoodDescription { get; set; } = string.Empty;
    [Range(0, 5000)]
    public int Calories { get; set; }
    public string? Notes { get; set; }
}

public class TrainerCommissionDashboardViewModel
{
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
    public List<TrainerCommissionRowViewModel> RecentCommissions { get; set; } = new();
}

public class TrainerCommissionRowViewModel
{
    public int Id { get; set; }
    public int MembershipId { get; set; }
    public int? BranchId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal CalculatedAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class TrainerFinancialDashboardViewModel
{
    public string TrainerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    public decimal TotalGeneratedCommission { get; set; }
    public decimal TotalPaidCommission { get; set; }
    public decimal TotalPendingCommission { get; set; }
    public decimal MembershipRevenueFromTrainerMembers { get; set; }
    public decimal SessionRevenue { get; set; }

    public List<TrainerFinancialCommissionRowViewModel> Commissions { get; set; } = new();
    public List<TrainerFinancialMembershipRevenueRowViewModel> MembershipRevenues { get; set; } = new();
    public List<TrainerFinancialSessionEarningRowViewModel> SessionEarnings { get; set; } = new();
    public List<TrainerFinancialTransactionRowViewModel> RecentTransactions { get; set; } = new();
    public List<TrainerFinancialTrendPointViewModel> CommissionLast30Days { get; set; } = new();
}

public class TrainerFinancialCommissionRowViewModel
{
    public string MemberName { get; set; } = string.Empty;
    public string MembershipPlanName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TrainerFinancialMembershipRevenueRowViewModel
{
    public string MemberName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal RevenueAmount { get; set; }
    public DateTime StartDate { get; set; }
}

public class TrainerFinancialSessionEarningRowViewModel
{
    public string SessionTitle { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal TrainerShare { get; set; }
    public DateTime Date { get; set; }
}

public class TrainerFinancialTransactionRowViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TrainerFinancialTrendPointViewModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}


