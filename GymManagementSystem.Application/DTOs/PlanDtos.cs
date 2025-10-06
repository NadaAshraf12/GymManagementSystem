namespace GymManagementSystem.Application.DTOs;

public class CreateTrainingPlanDto
{
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateTrainingPlanItemDto> Items { get; set; } = new();
}

public class CreateTrainingPlanItemDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public class CreateNutritionPlanDto
{
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateNutritionPlanItemDto> Items { get; set; } = new();
}

public class CreateNutritionPlanItemDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string FoodDescription { get; set; } = string.Empty;
    public int Calories { get; set; }
    public string? Notes { get; set; }
}


