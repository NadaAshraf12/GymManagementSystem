using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
