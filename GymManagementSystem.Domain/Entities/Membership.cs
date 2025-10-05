using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{
    public class Membership : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty; // Changed to string for Identity
        public int MembershipPlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal PaidAmount { get; set; }

        // Navigation Properties
        public virtual Member Member { get; set; } = null!;
        public virtual MembershipPlan MembershipPlan { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
