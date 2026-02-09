using System;
using GymManagementSystem.Domain.Enums;

namespace GymManagementSystem.Application.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class ChatConversationDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
