using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PaymentMethod).IsRequired();
        builder.Property(p => p.PaymentStatus).IsRequired();
        builder.Property(p => p.PaymentProofUrl).HasMaxLength(500);
        builder.Property(p => p.RejectionReason).HasMaxLength(500);
        builder.Property(p => p.ConfirmedByAdminId).HasMaxLength(450);

        builder.HasIndex(p => new { p.MembershipId, p.PaymentStatus, p.CreatedAt });
        builder.HasIndex(p => p.PaidAt);
    }
}
