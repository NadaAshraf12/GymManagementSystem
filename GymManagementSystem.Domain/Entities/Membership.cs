using GymManagementSystem.Domain.Entities.Base;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Domain.Entities
{
    public class Membership : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty;
        public int MembershipPlanId { get; set; }
        public int? BranchId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MembershipStatus Status { get; set; } = MembershipStatus.PendingPayment;
        public MembershipSource Source { get; set; } = MembershipSource.Online;
        public bool AutoRenewEnabled { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? FreezeStartDate { get; set; }
        public DateTime? FreezeEndDate { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalanceUsedFromWallet { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public virtual Member Member { get; set; } = null!;
        public virtual MembershipPlan MembershipPlan { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
