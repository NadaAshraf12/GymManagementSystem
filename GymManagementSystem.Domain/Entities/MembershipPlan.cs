using GymManagementSystem.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class MembershipPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxEntriesPerDay { get; set; }

        // Navigation Properties
        public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}
