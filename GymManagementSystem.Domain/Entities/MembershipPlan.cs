using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{
    public class MembershipPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CommissionRate { get; set; }
        public int DurationInDays { get; set; }
        public int IncludedSessionsPerMonth { get; set; }
        public decimal SessionDiscountPercentage { get; set; }
        public bool PriorityBooking { get; set; }
        public bool AddOnAccess { get; set; } = true;
        public bool IsDeleted { get; set; }

        public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}
