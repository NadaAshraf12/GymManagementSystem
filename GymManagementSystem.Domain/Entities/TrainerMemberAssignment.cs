using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class TrainerMemberAssignment : BaseEntity
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public virtual Trainer Trainer { get; set; } = null!;
    public virtual Member Member { get; set; } = null!;
}


