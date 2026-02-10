using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
        builder.Property(rt => rt.IpAddress).HasMaxLength(45);
        builder.Property(rt => rt.UserAgent).HasMaxLength(500);
    }
}