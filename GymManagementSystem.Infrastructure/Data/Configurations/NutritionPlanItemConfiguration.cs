using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class NutritionPlanItemConfiguration : IEntityTypeConfiguration<NutritionPlanItem>
{
    public void Configure(EntityTypeBuilder<NutritionPlanItem> builder)
    {
        builder.Property(i => i.MealType).IsRequired().HasMaxLength(100);
        builder.Property(i => i.FoodDescription).IsRequired().HasMaxLength(500);

        builder.HasOne(i => i.NutritionPlan)
            .WithMany(np => np.Items)
            .HasForeignKey(i => i.NutritionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}