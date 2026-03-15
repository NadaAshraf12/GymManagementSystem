using GymManagementSystem.Domain.Entities;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IChatRepository
    {
        Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task<ChatMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<ChatMessage>> GetChatHistoryAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
        Task<List<ChatMessage>> GetUnreadForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<ChatMessage?> GetLatestMessageAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
    }
}
