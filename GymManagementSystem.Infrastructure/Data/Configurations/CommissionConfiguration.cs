using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.Property(x => x.Percentage).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CalculatedAmount).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => new { x.TrainerId, x.IsPaid });
        builder.HasIndex(x => new { x.MembershipId, x.Source }).IsUnique();
        builder.HasIndex(x => new { x.MembershipId, x.CreatedAt });
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => x.PaidAt);

        builder.HasOne(x => x.Trainer)
            .WithMany(t => t.Commissions)
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Membership)
            .WithMany(m => m.Commissions)
            .HasForeignKey(x => x.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
