using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{
    public class MemberSession : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty; 
        public int WorkoutSessionId { get; set; }
        public DateTime BookingDate { get; set; }
        public bool Attended { get; set; }

        
        public virtual Member Member { get; set; } = null!;
        public virtual WorkoutSession WorkoutSession { get; set; } = null!;
    }
}
