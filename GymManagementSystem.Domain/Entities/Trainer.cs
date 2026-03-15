using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class Trainer : ApplicationUser
    {
        public string Specialty { get; set; } = string.Empty;
        public string Certification { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public int? BranchId { get; set; }

        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
        public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();
        public virtual Branch? Branch { get; set; }
    }
}
