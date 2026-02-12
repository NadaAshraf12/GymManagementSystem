using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasIndex(m => m.MemberCode).IsUnique();
        builder.Property(m => m.MemberCode).IsRequired().HasMaxLength(20);
        builder.Property(m => m.WalletBalance).HasColumnType("decimal(18,2)");
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasOne(m => m.Branch)
            .WithMany(b => b.Members)
            .HasForeignKey(m => m.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_Member_WalletBalance_NonNegative", "[WalletBalance] >= 0"));
    }
}
