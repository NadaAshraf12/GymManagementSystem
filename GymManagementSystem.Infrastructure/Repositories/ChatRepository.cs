using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _db;
        public ChatRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            await _db.ChatMessages.AddAsync(message, cancellationToken);
        }

        public async Task<ChatMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
        {
            return await _db.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChatMessage>> GetUnreadForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _db.ChatMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ChatMessage?> GetLatestMessageAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
        {
            return await _db.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
        {
            return await _db.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && m.SenderId == otherUserId && !m.IsRead, cancellationToken);
        }
    }
}
