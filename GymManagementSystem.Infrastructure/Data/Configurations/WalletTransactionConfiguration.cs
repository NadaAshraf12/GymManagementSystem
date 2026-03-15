using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Member)
            .WithMany(m => m.WalletTransactions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MemberId, x.CreatedAt });
        builder.HasIndex(x => new { x.Type, x.CreatedAt });
        builder.HasIndex(x => x.ReferenceId);
    }
}
