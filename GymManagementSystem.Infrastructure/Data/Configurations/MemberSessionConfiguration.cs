using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class MemberSessionConfiguration : IEntityTypeConfiguration<MemberSession>
{
    public void Configure(EntityTypeBuilder<MemberSession> builder)
    {
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