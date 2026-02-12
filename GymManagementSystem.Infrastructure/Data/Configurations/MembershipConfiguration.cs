using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.Property(m => m.TotalPaid).HasColumnType("decimal(18,2)");
        builder.Property(m => m.RemainingBalanceUsedFromWallet).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Status).IsRequired();
        builder.Property(m => m.Source).IsRequired();
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.HasOne(m => m.Member)
            .WithMany(m => m.Memberships)
            .HasForeignKey(m => m.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.MembershipPlan)
            .WithMany(mp => mp.Memberships)
            .HasForeignKey(m => m.MembershipPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Branch)
            .WithMany(b => b.Memberships)
            .HasForeignKey(m => m.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Payments)
            .WithOne(p => p.Membership)
            .HasForeignKey(p => p.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
