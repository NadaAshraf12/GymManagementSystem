using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class LoginAuditConfiguration : IEntityTypeConfiguration<LoginAudit>
{
    public void Configure(EntityTypeBuilder<LoginAudit> builder)
    {
        builder.HasOne(la => la.User)
            .WithMany()
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(la => la.IpAddress).HasMaxLength(45);
        builder.Property(la => la.UserAgent).HasMaxLength(500);
        builder.Property(la => la.FailureReason).HasMaxLength(200);
    }
}