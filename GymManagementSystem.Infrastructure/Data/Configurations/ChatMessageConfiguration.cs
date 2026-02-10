using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementSystem.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.Property(m => m.Message).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.AttachmentUrl).HasMaxLength(500);

        builder.HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentAt });
        builder.HasIndex(m => new { m.ReceiverId, m.IsRead });

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}