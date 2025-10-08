using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{
    public class MembershipPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxEntriesPerDay { get; set; }

        public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}
