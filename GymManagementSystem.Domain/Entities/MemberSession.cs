using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{
    public class MemberSession : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty; 
        public int WorkoutSessionId { get; set; }
        public DateTime BookingDate { get; set; }
        public bool Attended { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal ChargedPrice { get; set; }
        public decimal AppliedDiscountPercentage { get; set; }
        public bool UsedIncludedSession { get; set; }
        public bool PriorityBookingApplied { get; set; }

        
        public virtual Member Member { get; set; } = null!;
        public virtual WorkoutSession WorkoutSession { get; set; } = null!;
    }
}
