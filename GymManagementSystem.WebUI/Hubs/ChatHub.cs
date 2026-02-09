using System.Collections.Concurrent;
using System.Security.Claims;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GymManagementSystem.WebUI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();
        private static readonly ConcurrentDictionary<string, string?> ConnectionOpenWith = new();

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                ConnectionUsers[Context.ConnectionId] = userId;
                ConnectionOpenWith[Context.ConnectionId] = null;

                var unread = await _chatService.GetUnreadMessagesAsync(userId);
                if (unread.Count > 0)
                {
                    await Clients.Caller.SendAsync("ReceiveUnreadMessages", unread);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectionUsers.TryRemove(Context.ConnectionId, out _);
            ConnectionOpenWith.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task OpenConversation(string withUserId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new HubException("Unauthorized");
            }

            ConnectionOpenWith[Context.ConnectionId] = withUserId;

            try
            {
                var readIds = await _chatService.MarkAsReadByConversationAsync(withUserId, currentUserId);
                if (readIds.Count > 0)
                {
                    await Clients.User(withUserId).SendAsync("MessageRead", new { messageIds = readIds, readerId = currentUserId });
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = GetUserId();
            if (string.IsNullOrWhiteSpace(senderId))
            {
                throw new HubException("Unauthorized");
            }

            try
            {
                var dto = await _chatService.SendMessageAsync(senderId, receiverId, message);

                var receiverHasOpenConversation = ConnectionUsers
                    .Where(kvp => kvp.Value == receiverId)
                    .Select(kvp => kvp.Key)
                    .Any(connId =>
                    {
                        if (!ConnectionOpenWith.TryGetValue(connId, out var openWith)) return false;
                        return string.Equals(openWith, senderId, StringComparison.Ordinal);
                    });

                if (receiverHasOpenConversation)
                {
                    var readIds = await _chatService.MarkAsReadByConversationAsync(senderId, receiverId);
                    if (readIds.Contains(dto.Id))
                    {
                        dto.IsRead = true;
                    }

                    if (readIds.Count > 0)
                    {
                        await Clients.User(senderId).SendAsync("MessageRead", new { messageIds = readIds, readerId = receiverId });
                    }
                }

                await Clients.User(receiverId).SendAsync("ReceiveMessage", dto);
                await Clients.Caller.SendAsync("ReceiveMessage", dto);
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Unauthorized");
            }

            await _chatService.MarkAsReadAsync(userId, messageId);
        }

        public async Task<IReadOnlyList<ChatMessageDto>> GetChatHistory(string userId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new HubException("Unauthorized");
            }

            try
            {
                var history = await _chatService.GetChatHistoryAsync(currentUserId, userId);
                return history;
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        private string? GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
