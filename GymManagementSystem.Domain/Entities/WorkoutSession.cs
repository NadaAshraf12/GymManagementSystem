using GymManagementSystem.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class WorkoutSession : BaseEntity
    {
        public string TrainerId { get; set; } = string.Empty; // Changed to string
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }

        // Navigation Properties
        public virtual Trainer Trainer { get; set; } = null!;
        public virtual ICollection<MemberSession> MemberSessions { get; set; } = new List<MemberSession>();
    }
}
