using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.WebUI.Models;

public class TrainerMemberListItem
{
    public string MemberId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
}

public class CreateTrainingPlanViewModel
{
    [Required]
    public string MemberId { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateTrainingPlanItemViewModel> Items { get; set; } = new();
}

public class CreateTrainingPlanItemViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public string ExerciseName { get; set; } = string.Empty;
    [Range(1, 20)]
    public int Sets { get; set; }
    [Range(1, 100)]
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public class CreateNutritionPlanViewModel
{
    [Required]
    public string MemberId { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateNutritionPlanItemViewModel> Items { get; set; } = new();
}

public class CreateNutritionPlanItemViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public string MealType { get; set; } = string.Empty;
    [Required]
    [MaxLength(500)]
    public string FoodDescription { get; set; } = string.Empty;
    [Range(0, 5000)]
    public int Calories { get; set; }
    public string? Notes { get; set; }
}


