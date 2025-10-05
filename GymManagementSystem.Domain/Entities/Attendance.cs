using GymManagementSystem.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class Attendance : BaseEntity
    {
        public string MemberId { get; set; } = string.Empty; // Changed to string
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public DateTime Date { get; set; }

        // Navigation Properties
        public virtual Member Member { get; set; } = null!;
    }
}
