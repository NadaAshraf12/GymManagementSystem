namespace GymManagementSystem.Application.DTOs;

public class CreateTrainingPlanDto
{
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateTrainingPlanItemDto> Items { get; set; } = new();
}

public class UpdateTrainingPlanDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateTrainingPlanItemDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTrainingPlanItemDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}

public class CreateNutritionPlanDto
{
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateNutritionPlanItemDto> Items { get; set; } = new();
}

public class UpdateNutritionPlanDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateNutritionPlanItemDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string FoodDescription { get; set; } = string.Empty;
    public int Calories { get; set; }
    public string? Notes { get; set; }
}

public class UpdateNutritionPlanItemDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string FoodDescription { get; set; } = string.Empty;
    public int Calories { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}

public class TrainingPlanDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TrainingPlanItemDto> Items { get; set; } = new();
}

public class TrainingPlanItemDto
{
    public int Id { get; set; }
    public int TrainingPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}

public class NutritionPlanDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<NutritionPlanItemDto> Items { get; set; } = new();
}

public class NutritionPlanItemDto
{
    public int Id { get; set; }
    public int NutritionPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string FoodDescription { get; set; } = string.Empty;
    public int Calories { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}


