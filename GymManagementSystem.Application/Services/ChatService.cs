using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IApplicationDbContext _db;

        public ChatService(IChatRepository chatRepository, IApplicationDbContext db)
        {
            _chatRepository = chatRepository;
            _db = db;
        }

        public async Task<bool> CanChatAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
        {
            return await _db.TrainerMemberAssignments
                .AnyAsync(a => (a.TrainerId == userId && a.MemberId == otherUserId) ||
                               (a.TrainerId == otherUserId && a.MemberId == userId), cancellationToken);
        }

        public async Task<ChatMessageDto> SendMessageAsync(string senderId, string receiverId, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty.", nameof(message));
            }

            if (!await CanChatAsync(senderId, receiverId, cancellationToken))
            {
                throw new InvalidOperationException("You are not allowed to chat with this user.");
            }

            var entity = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false,
                Type = MessageType.Text,
                AttachmentUrl = null
            };

            await _chatRepository.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderId, cancellationToken);

            return new ChatMessageDto
            {
                Id = entity.Id,
                SenderId = entity.SenderId,
                ReceiverId = entity.ReceiverId,
                Message = entity.Message,
                SentAt = entity.SentAt,
                IsRead = entity.IsRead,
                SenderName = BuildDisplayName(sender),
                Type = entity.Type,
                AttachmentUrl = entity.AttachmentUrl
            };
        }

        public async Task<ChatMessageDto> SendAttachmentAsync(string senderId, string receiverId, string? message, MessageType type, string attachmentUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(attachmentUrl))
            {
                throw new ArgumentException("AttachmentUrl is required.", nameof(attachmentUrl));
            }

            if (!await CanChatAsync(senderId, receiverId, cancellationToken))
            {
                throw new InvalidOperationException("You are not allowed to chat with this user.");
            }

            var entity = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message?.Trim() ?? string.Empty,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                Type = type,
                AttachmentUrl = attachmentUrl
            };

            await _chatRepository.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderId, cancellationToken);

            return new ChatMessageDto
            {
                Id = entity.Id,
                SenderId = entity.SenderId,
                ReceiverId = entity.ReceiverId,
                Message = entity.Message,
                SentAt = entity.SentAt,
                IsRead = entity.IsRead,
                SenderName = BuildDisplayName(sender),
                Type = entity.Type,
                AttachmentUrl = entity.AttachmentUrl
            };
        }

        public async Task<IReadOnlyList<ChatMessageDto>> GetChatHistoryAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
        {
            if (!await CanChatAsync(userId, otherUserId, cancellationToken))
            {
                throw new InvalidOperationException("You are not allowed to chat with this user.");
            }

            var messages = await _chatRepository.GetChatHistoryAsync(userId, otherUserId, cancellationToken);
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            var users = await _db.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var nameMap = users.ToDictionary(u => u.Id, BuildDisplayName);

            return messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Message = m.Message,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                SenderName = nameMap.TryGetValue(m.SenderId, out var name) ? name : string.Empty,
                Type = m.Type,
                AttachmentUrl = m.AttachmentUrl
            }).ToList();
        }

        public async Task<IReadOnlyList<ChatMessageDto>> GetUnreadMessagesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var messages = await _chatRepository.GetUnreadForUserAsync(userId, cancellationToken);
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            var users = await _db.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var nameMap = users.ToDictionary(u => u.Id, BuildDisplayName);

            return messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Message = m.Message,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                SenderName = nameMap.TryGetValue(m.SenderId, out var name) ? name : string.Empty,
                Type = m.Type,
                AttachmentUrl = m.AttachmentUrl
            }).ToList();
        }

        public async Task MarkAsReadAsync(string userId, int messageId, CancellationToken cancellationToken = default)
        {
            var message = await _chatRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null) return;

            if (!string.Equals(message.ReceiverId, userId, StringComparison.Ordinal))
            {
                return;
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IReadOnlyList<int>> MarkAsReadByConversationAsync(string senderId, string receiverId, CancellationToken cancellationToken = default)
        {
            if (!await CanChatAsync(senderId, receiverId, cancellationToken))
            {
                throw new InvalidOperationException("You are not allowed to chat with this user.");
            }

            var messages = await _db.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0) return Array.Empty<int>();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return messages.Select(m => m.Id).ToList();
        }

        public async Task<IReadOnlyList<string>> GetRelatedUserIdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var trainerMembers = await _db.TrainerMemberAssignments
                .Where(a => a.TrainerId == userId)
                .Select(a => a.MemberId)
                .ToListAsync(cancellationToken);

            var memberTrainer = await _db.TrainerMemberAssignments
                .Where(a => a.MemberId == userId)
                .Select(a => a.TrainerId)
                .ToListAsync(cancellationToken);

            return trainerMembers.Concat(memberTrainer).Distinct().ToList();
        }

        public async Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var conversations = new List<ChatConversationDto>();

            var trainerAssignments = await _db.TrainerMemberAssignments
                .Include(a => a.Member)
                .Where(a => a.TrainerId == userId)
                .ToListAsync(cancellationToken);

            var memberAssignments = await _db.TrainerMemberAssignments
                .Include(a => a.Trainer)
                .Where(a => a.MemberId == userId)
                .ToListAsync(cancellationToken);

            foreach (var assignment in trainerAssignments)
            {
                var otherId = assignment.MemberId;
                var displayName = BuildDisplayName(assignment.Member);

                var last = await _chatRepository.GetLatestMessageAsync(userId, otherId, cancellationToken);
                var unread = await _chatRepository.GetUnreadCountAsync(userId, otherId, cancellationToken);

                conversations.Add(new ChatConversationDto
                {
                    UserId = otherId,
                    DisplayName = displayName,
                    LastMessage = last?.Message ?? string.Empty,
                    LastMessageAt = last?.SentAt,
                    UnreadCount = unread
                });
            }

            foreach (var assignment in memberAssignments)
            {
                var otherId = assignment.TrainerId;
                var displayName = BuildDisplayName(assignment.Trainer);

                var last = await _chatRepository.GetLatestMessageAsync(userId, otherId, cancellationToken);
                var unread = await _chatRepository.GetUnreadCountAsync(userId, otherId, cancellationToken);

                conversations.Add(new ChatConversationDto
                {
                    UserId = otherId,
                    DisplayName = displayName,
                    LastMessage = last?.Message ?? string.Empty,
                    LastMessageAt = last?.SentAt,
                    UnreadCount = unread
                });
            }

            return conversations
                .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
                .ToList();
        }

        private static string BuildDisplayName(ApplicationUser? user)
        {
            if (user == null) return string.Empty;
            var name = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? (user.UserName ?? string.Empty) : name;
        }
    }
}
