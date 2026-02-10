using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class TrainingPlanItemConfiguration : IEntityTypeConfiguration<TrainingPlanItem>
{
    public void Configure(EntityTypeBuilder<TrainingPlanItem> builder)
    {
        builder.Property(i => i.ExerciseName).IsRequired().HasMaxLength(200);

        builder.HasOne(i => i.TrainingPlan)
            .WithMany(tp => tp.Items)
            .HasForeignKey(i => i.TrainingPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}