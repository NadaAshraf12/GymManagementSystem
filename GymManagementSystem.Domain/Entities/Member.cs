using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities
{

    public class Member : ApplicationUser
    {
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string MedicalConditions { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public string MemberCode { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }


        public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public virtual ICollection<MemberSession> MemberSessions { get; set; } = new List<MemberSession>();
    }
}
