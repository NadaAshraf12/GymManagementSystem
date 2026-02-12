using GymManagementSystem.Domain.Entities.Base;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int MembershipId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VodafoneCash;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string? PaymentProofUrl { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? ConfirmedByAdminId { get; set; }

        public virtual Membership Membership { get; set; } = null!;
    }
}
