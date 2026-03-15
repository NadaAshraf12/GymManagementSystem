using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.Property(mp => mp.Name).IsRequired().HasMaxLength(100);
        builder.Property(mp => mp.Description).HasMaxLength(1000);
        builder.Property(mp => mp.DurationInDays).IsRequired();
        builder.Property(mp => mp.Price).HasColumnType("decimal(18,2)");
        builder.Property(mp => mp.CommissionRate).HasColumnType("decimal(5,2)");
        builder.Property(mp => mp.IncludedSessionsPerMonth).IsRequired();
        builder.Property(mp => mp.SessionDiscountPercentage).HasColumnType("decimal(5,2)");
        builder.HasIndex(mp => new { mp.Name, mp.IsDeleted });
    }
}
