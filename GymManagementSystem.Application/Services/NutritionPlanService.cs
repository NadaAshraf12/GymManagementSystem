using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;

namespace GymManagementSystem.Application.Services;

public class NutritionPlanService : INutritionPlanService
{
    private readonly IApplicationDbContext _db;
    public NutritionPlanService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateNutritionPlanDto dto)
    {
        var plan = new NutritionPlan
        {
            MemberId = dto.MemberId,
            TrainerId = dto.TrainerId,
            Title = dto.Title,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        _db.NutritionPlans.Add(plan);
        await _db.SaveChangesAsync();

        if (dto.Items.Count > 0)
        {
            foreach (var item in dto.Items)
            {
                _db.NutritionPlanItems.Add(new NutritionPlanItem
                {
                    NutritionPlanId = plan.Id,
                    DayOfWeek = item.DayOfWeek,
                    MealType = item.MealType,
                    FoodDescription = item.FoodDescription,
                    Calories = item.Calories,
                    Notes = item.Notes
                });
            }
            await _db.SaveChangesAsync();
        }

        return plan.Id;
    }
}


