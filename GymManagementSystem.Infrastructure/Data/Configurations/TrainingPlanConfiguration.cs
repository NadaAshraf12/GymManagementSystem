using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.Property(tp => tp.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(tp => tp.Member)
            .WithMany()
            .HasForeignKey(tp => tp.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tp => tp.Trainer)
            .WithMany()
            .HasForeignKey(tp => tp.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}