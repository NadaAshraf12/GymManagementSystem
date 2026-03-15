using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class AddOn : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? BranchId { get; set; }
    public bool RequiresActiveMembership { get; set; }

    public virtual Branch? Branch { get; set; }
}
