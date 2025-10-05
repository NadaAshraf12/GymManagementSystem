using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class TrainingPlanItem : BaseEntity
{
    public int Id { get; set; }
    public int TrainingPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }

    public virtual TrainingPlan TrainingPlan { get; set; } = null!;
}


