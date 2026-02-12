using GymManagementSystem.Domain.Entities.Base;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Domain.Entities;

public class Commission : BaseEntity
{
    public string TrainerId { get; set; } = string.Empty;
    public int MembershipId { get; set; }
    public int? BranchId { get; set; }
    public CommissionSource Source { get; set; } = CommissionSource.Activation;
    public decimal Percentage { get; set; }
    public decimal CalculatedAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidByAdminId { get; set; }

    public virtual Trainer Trainer { get; set; } = null!;
    public virtual Membership Membership { get; set; } = null!;
}
