using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class NutritionPlan : BaseEntity
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Member Member { get; set; } = null!;
    public virtual Trainer Trainer { get; set; } = null!;
    public virtual ICollection<NutritionPlanItem> Items { get; set; } = new List<NutritionPlanItem>();
}


