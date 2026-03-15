using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class MemberSessionConfiguration : IEntityTypeConfiguration<MemberSession>
{
    public void Configure(EntityTypeBuilder<MemberSession> builder)
    {
        builder.Property(ms => ms.OriginalPrice).HasColumnType("decimal(18,2)");
        builder.Property(ms => ms.ChargedPrice).HasColumnType("decimal(18,2)");
        builder.Property(ms => ms.AppliedDiscountPercentage).HasColumnType("decimal(5,2)");
        builder.HasIndex(ms => new { ms.MemberId, ms.BookingDate, ms.UsedIncludedSession });

        builder.HasOne(ms => ms.Member)
            .WithMany(m => m.MemberSessions)
            .HasForeignKey(ms => ms.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ms => ms.WorkoutSession)
            .WithMany(ws => ws.MemberSessions)
            .HasForeignKey(ms => ms.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
