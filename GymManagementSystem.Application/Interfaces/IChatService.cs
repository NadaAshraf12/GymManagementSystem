using GymManagementSystem.Application.DTOs;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IChatService
    {
        Task<bool> CanChatAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
        Task<ChatMessageDto> SendMessageAsync(string senderId, string receiverId, string message, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatMessageDto>> GetChatHistoryAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatMessageDto>> GetUnreadMessagesAsync(string userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(string userId, int messageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<int>> MarkAsReadByConversationAsync(string senderId, string receiverId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatConversationDto>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default);
    }
}
