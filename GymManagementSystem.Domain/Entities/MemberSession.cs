using GymManagementSystem.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class MemberSession : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty; // Changed to string
        public int WorkoutSessionId { get; set; }
        public DateTime BookingDate { get; set; }
        public bool Attended { get; set; }

        // Navigation Properties
        public virtual Member Member { get; set; } = null!;
        public virtual WorkoutSession WorkoutSession { get; set; } = null!;
    }
}
