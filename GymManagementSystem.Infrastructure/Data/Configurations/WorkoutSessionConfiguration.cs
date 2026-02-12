using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.Property(ws => ws.Price).HasColumnType("decimal(18,2)");

        builder.HasOne(ws => ws.Trainer)
            .WithMany(t => t.WorkoutSessions)
            .HasForeignKey(ws => ws.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.Branch)
            .WithMany(b => b.WorkoutSessions)
            .HasForeignKey(ws => ws.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
