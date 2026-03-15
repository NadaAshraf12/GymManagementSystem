using System;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Domain.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string? AttachmentUrl { get; set; }

        public virtual ApplicationUser Sender { get; set; } = null!;
        public virtual ApplicationUser Receiver { get; set; } = null!;
    }
}
