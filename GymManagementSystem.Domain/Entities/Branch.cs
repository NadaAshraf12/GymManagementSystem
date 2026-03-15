using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
    public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
}
