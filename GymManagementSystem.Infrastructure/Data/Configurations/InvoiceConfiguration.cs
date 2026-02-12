using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FilePath).IsRequired().HasMaxLength(600);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.MemberId, x.CreatedAt });
        builder.HasIndex(x => x.MembershipId);
        builder.HasIndex(x => x.AddOnId);
        builder.HasIndex(x => x.PaymentId);

        builder.HasOne(x => x.Member)
            .WithMany(m => m.Invoices)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Membership)
            .WithMany(m => m.Invoices)
            .HasForeignKey(x => x.MembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AddOn)
            .WithMany()
            .HasForeignKey(x => x.AddOnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
