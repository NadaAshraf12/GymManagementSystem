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
        public decimal WalletBalance { get; set; }
        public int? BranchId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public DateTime? UpdatedAt { get; set; }


        public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public virtual ICollection<MemberSession> MemberSessions { get; set; } = new List<MemberSession>();
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual Branch? Branch { get; set; }
    }
}
