using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class NutritionPlanConfiguration : IEntityTypeConfiguration<NutritionPlan>
{
    public void Configure(EntityTypeBuilder<NutritionPlan> builder)
    {
        builder.Property(np => np.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(np => np.Member)
            .WithMany()
            .HasForeignKey(np => np.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(np => np.Trainer)
            .WithMany()
            .HasForeignKey(np => np.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}