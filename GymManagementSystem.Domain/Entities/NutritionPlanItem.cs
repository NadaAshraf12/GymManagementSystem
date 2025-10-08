using GymManagementSystem.Domain.Entities.Base;

namespace GymManagementSystem.Domain.Entities;

public class NutritionPlanItem : BaseEntity
{
    public int Id { get; set; }
    public int NutritionPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string MealType { get; set; } = string.Empty; 
    public string FoodDescription { get; set; } = string.Empty;
    public int Calories { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }

    public virtual NutritionPlan NutritionPlan { get; set; } = null!;
}


