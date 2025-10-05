using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementSystem.Domain.Entities
{
    public class Trainer : ApplicationUser
    {
        // Trainer-specific properties
        public string Specialty { get; set; } = string.Empty;
        public string Certification { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    }
}
