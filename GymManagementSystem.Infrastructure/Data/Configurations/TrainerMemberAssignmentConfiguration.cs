using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class TrainerMemberAssignmentConfiguration : IEntityTypeConfiguration<TrainerMemberAssignment>
{
    public void Configure(EntityTypeBuilder<TrainerMemberAssignment> builder)
    {
        builder.HasIndex(x => new { x.TrainerId, x.MemberId }).IsUnique();
        builder.HasIndex(x => x.MemberId).IsUnique();

        builder.HasOne(a => a.Trainer)
            .WithMany()
            .HasForeignKey(a => a.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}