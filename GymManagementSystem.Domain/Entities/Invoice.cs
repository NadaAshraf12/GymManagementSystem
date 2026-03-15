using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? MembershipId { get; set; }
    public int? AddOnId { get; set; }
    public int? PaymentId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public virtual Membership? Membership { get; set; }
    public virtual AddOn? AddOn { get; set; }
    public virtual Payment? Payment { get; set; }
    public virtual Member Member { get; set; } = null!;
}
